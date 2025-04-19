using Repository.DTOs;
using Repository.Interfaces;
using Repository.Models;
using Service.Helpers;
using Service.Interfaces;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAuthRepository _authRepo;
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IVoucherRepository _voucherRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public OrderService(
            IAuthRepository authRepo,
            ICartRepository cartRepo,
            IOrderRepository orderRepo,
            IVoucherRepository voucherRepo,
            IPaymentRepository paymentRepo,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _authRepo = authRepo;
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _voucherRepo = voucherRepo;
            _paymentRepo = paymentRepo;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<PayPalApprovalDto> CreateOrderWithPaypalAsync(string userEmail, OrderRequestDto dto)
        {
            var user = await _authRepo.GetUserByEmailAsync(userEmail);
            if (user == null) throw new Exception("User không tồn tại");

            var cart = await _cartRepo.GetOrCreateCartAsync(user.Id);
            var cartItems = await _cartRepo.GetCartItemsAsync(cart.Id);
            if (cartItems.Count == 0) throw new Exception("Giỏ hàng trống");

            decimal total = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Product == null) continue;
                if (item.Product.Stock < item.Quantity)
                    throw new Exception($"Sản phẩm {item.Product.Name} không đủ hàng trong kho");

                total += item.Quantity * item.Product.Price;
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            Voucher? voucher = null;
            if (!string.IsNullOrEmpty(dto.VoucherCode))
            {
                voucher = await _voucherRepo.GetByCodeAsync(dto.VoucherCode);
                if (voucher != null)
                {
                    total -= (total * (voucher.Discount / 100));
                }
            }

            // Gọi PayPal
            var paypalClient = _httpClientFactory.CreateClient();
            var accessToken = await GetAccessTokenAsync(paypalClient);
            paypalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var returnUrl = _config["PayPal:ReturnUrl"];
            var cancelUrl = _config["PayPal:CancelUrl"];
            var currency = "USD";

            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency,
                            value = total.ToString("F2")
                        }
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await paypalClient.PostAsync($"{_config["PayPal:BaseUrl"]}/v2/checkout/orders", content);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception("Lỗi tạo đơn PayPal: " + json);

            var result = JsonSerializer.Deserialize<JsonElement>(json);
            var approvalLink = result.GetProperty("links").EnumerateArray()
                .FirstOrDefault(link => link.GetProperty("rel").GetString() == "approve")
                .GetProperty("href").GetString();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Status = "pending",
                VoucherId = voucher?.Id,
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };
            await _orderRepo.CreateOrderAsync(order);

            await _paymentRepo.CreatePaymentAsync(new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TransactionId = result.GetProperty("id").GetString(),
                PaymentMethod = "paypal",
                PaymentStatus = "pending",
                CreatedAt = DateTime.UtcNow
            });

            return new PayPalApprovalDto
            {
                ApprovalUrl = approvalLink!,
                OrderId = order.Id.ToString()
            };
        }

        public async Task<bool> ConfirmOrderPaymentAsync(string paypalOrderId)
        {
            var paypalClient = _httpClientFactory.CreateClient();
            var accessToken = await GetAccessTokenAsync(paypalClient);
            paypalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var captureResponse = await paypalClient.PostAsync(
     $"{_config["PayPal:BaseUrl"]}/v2/checkout/orders/{paypalOrderId}/capture", content);
            if (!captureResponse.IsSuccessStatusCode)
            {
                var errorJson = await captureResponse.Content.ReadAsStringAsync();
                throw new Exception("Lỗi capture: " + errorJson);
            }


            await _paymentRepo.UpdatePaymentStatusAsync(paypalOrderId, "completed");

            // update order status
            var order = await _orderRepo.GetOrderByTransactionId(paypalOrderId);
            if (order != null)
            {
                await _orderRepo.UpdateOrderStatusAsync(order.Id, "completed");

                // trừ stock
                foreach (var item in order.OrderItems)
                {
                    var product = await _cartRepo.GetProductByIdAsync(item.ProductId.Value);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        await _cartRepo.SaveProductAsync(product);
                        await _cartRepo.ClearCartAsync(order.UserId!.Value);

                    }
                }
            }

            return true;
        }

        private async Task<string> GetAccessTokenAsync(HttpClient client)
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];

            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{secret}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var body = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.PostAsync($"{_config["PayPal:BaseUrl"]}/v1/oauth2/token", body);
            var result = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<JsonElement>(result);
            return json.GetProperty("access_token").GetString()!;
        }
    }
}

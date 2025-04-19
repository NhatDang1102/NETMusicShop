using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTOs;
using Service.Interfaces;
using System.Security.Claims;

namespace MainApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "customer")] 
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("create-paypal")]
        public async Task<IActionResult> CreateWithPaypal([FromBody] OrderRequestDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var result = await _orderService.CreateOrderWithPaypalAsync(userEmail, dto);
            return Ok(result);
        }

        [HttpGet("paypal-success")]
        [AllowAnonymous]
        public async Task<IActionResult> PaypalSuccess([FromQuery] string token)
        {
            var success = await _orderService.ConfirmOrderPaymentAsync(token);
            if (!success) return BadRequest("Xác nhận thanh toán thất bại.");
            return Ok("Thanh toán thành công!");
        }

        [HttpGet("paypal-cancel")]
        [AllowAnonymous]
        public IActionResult PaypalCancel()
        {
            return Ok("Đã huỷ thanh toán.");
        }
    }
}

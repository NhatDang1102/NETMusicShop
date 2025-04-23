using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Service.Helpers;
using Service.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Service.Services
{
    public class ChatService : IChatService
    {
        private readonly OpenAiSettings _openAiSettings;
        private readonly GoogleSearchSettings _googleSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProductService _productService;

        public ChatService(
            IOptions<OpenAiSettings> openAiOptions,
            IOptions<GoogleSearchSettings> googleOptions,
            IHttpClientFactory httpClientFactory,
            IProductService productService)
        {
            _openAiSettings = openAiOptions.Value;
            _googleSettings = googleOptions.Value;
            _httpClientFactory = httpClientFactory;
            _productService = productService;
        }

        public async Task<string> AskAssistantAsync(string prompt, IFormFile? image)
        {
            // Check stock before calling AI
            var stockResponse = await TryRespondFromStock(prompt);
            if (!string.IsNullOrEmpty(stockResponse))
                return stockResponse;

            // Prepare request for OpenAI
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiSettings.ApiKey);

            var contentArray = new List<object>();

            if (image != null)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                var base64Image = Convert.ToBase64String(ms.ToArray());
                contentArray.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:image/png;base64,{base64Image}" }
                });
            }

            contentArray.Add(new { type = "text", text = prompt });

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Bạn là trợ lý AI chuyên hỗ trợ khách hàng về tai nghe và máy nghe nhạc, tên là Tiên nữ Âm nhạc Mumusa. Nếu người dùng hỏi hình ảnh, chỉ trả lời phần nội dung, không kèm link. Link ảnh sẽ được hệ thống xử lý riêng. Đừng trả lời kiểu 'không thể cung cấp hình', hãy trả lời nội dung. Luôn mở đầu bằng 'Mumusha xin mạn phép trả lời tiện nữ'. Chủ shop là Đặng Lê Minh Nhật."
                },
                new { role = "user", content = contentArray }
            };

            var body = new
            {
                model = _openAiSettings.Model,
                messages = messages
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var reply = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // Add image if related
            if (ContainsImageKeyword(prompt))
            {
                var img = await SearchGoogleImageAsync(prompt);
                if (!string.IsNullOrEmpty(img))
                    reply += $"\n\nĐây là hình ảnh minh hoạ bạn có thể tham khảo:\n{img}";
            }

            return reply!;
        }

        private async Task<string?> TryRespondFromStock(string prompt)
        {
            var lower = prompt.ToLower();

            var triggerWords = new[] { "còn hàng", "còn không", "trong kho", "còn bao nhiêu", "hết chưa", "còn nhiêu", "còn kg", "còn bn", "hết chưa", "stock", "bao nhiêu cái" };

            if (!triggerWords.Any(k => lower.Contains(k)))
                return null;

            var allProducts = await _productService.GetAllAsync();
            var matchedProducts = allProducts.Where(p => lower.Contains(p.Name.ToLower())).ToList();

            if (matchedProducts.Count == 0) return null;

            var replies = matchedProducts.Select(p =>
            {
                var quantity = p.Stock ?? 0;
                return quantity > 0
                    ? $"Sản phẩm \"{p.Name}\" còn {quantity} cái trong kho."
                    : $"Rất tiếc, \"{p.Name}\" đã hết hàng.";
            });

            return "Mumusha xin mạn phép trả lời tiện nữ:\n" + string.Join("\n", replies);
        }

        private bool ContainsImageKeyword(string prompt)
        {
            var p = prompt.ToLower();
            return p.Contains("ảnh") || p.Contains("hình") || p.Contains("image") || p.Contains("photo");
        }

        private async Task<string?> SearchGoogleImageAsync(string query)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://www.googleapis.com/customsearch/v1?key={_googleSettings.ApiKey}" +
                      $"&cx={_googleSettings.SearchEngineId}&searchType=image&q={Uri.EscapeDataString(query)}";

            var res = await client.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            if (result.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                return items[0].GetProperty("link").GetString();
            }

            return null;
        }

        public async Task<string> GenerateImageFromPromptAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiSettings.ApiKey);

            var body = new
            {
                prompt = prompt,
                n = 1,
                size = "512x512"
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/images/generations", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var imageUrl = result.GetProperty("data")[0].GetProperty("url").GetString();

            return imageUrl!;
        }
    }
}

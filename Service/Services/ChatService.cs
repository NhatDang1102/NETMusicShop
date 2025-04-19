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

        public ChatService(
            IOptions<OpenAiSettings> openAiOptions,
            IOptions<GoogleSearchSettings> googleOptions,
            IHttpClientFactory httpClientFactory)
        {
            _openAiSettings = openAiOptions.Value;
            _googleSettings = googleOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> AskAssistantAsync(string prompt, IFormFile? image)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _openAiSettings.ApiKey);

            var contentArray = new List<object>();

            // encode hình -> base64
            if (image != null)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                var base64Image = Convert.ToBase64String(ms.ToArray());

                contentArray.Add(new
                {
                    type = "image_url",
                    image_url = new
                    {
                        url = $"data:image/png;base64,{base64Image}"
                    }
                });
            }

            // text prompt
            contentArray.Add(new
            {
                type = "text",
                text = prompt
            });

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Bạn là trợ lý AI chuyên hỗ trợ khách hàng về tai nghe và máy nghe nhạc, tên là Tiên nữ Âm nhạc Mumusa. Nếu người dùng hỏi hình ảnh, chỉ trả lời phần nội dung, không kèm link. Link ảnh sẽ được hệ thống xử lý riêng."
                },
                new
                {
                    role = "user",
                    content = contentArray
                }
            };

            var requestBody = new
            {
                model = _openAiSettings.Model,
                messages = messages
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var reply = result.GetProperty("choices")[0]
                              .GetProperty("message")
                              .GetProperty("content")
                              .GetString();

            //search nếu có từ khóa
            if (prompt.ToLower().Contains("hình") || prompt.ToLower().Contains("ảnh") || prompt.ToLower().Contains("image"))
            {
                var imageLink = await SearchGoogleImageAsync(prompt);
                if (!string.IsNullOrEmpty(imageLink))
                {
                    reply += $"\n\n Đây là hình ảnh minh hoạ bạn có thể tham khảo:\n{imageLink}";
                }
            }

            return reply!;
        }

        private async Task<string?> SearchGoogleImageAsync(string query)
        {
            var searchClient = _httpClientFactory.CreateClient();
            var url = $"https://www.googleapis.com/customsearch/v1?key={_googleSettings.ApiKey}" +
                      $"&cx={_googleSettings.SearchEngineId}&searchType=image&q={Uri.EscapeDataString(query)}";

            var res = await searchClient.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            if (!result.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            {
                return null;
            }

            return items[0].GetProperty("link").GetString();
        }
    }
}

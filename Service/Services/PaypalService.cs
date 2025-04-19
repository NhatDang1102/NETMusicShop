using Microsoft.Extensions.Options;
using Service.Helpers;
using Service.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Service.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly PayPalSettings _settings;
        private readonly HttpClient _httpClient;

        public PayPalService(IOptions<PayPalSettings> settings)
        {
            _settings = settings.Value;
            _httpClient = new HttpClient();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var authToken = Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.Secret}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v1/oauth2/token", content);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<string> CreatePaymentUrl(decimal totalAmount)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = "USD",
                            value = totalAmount.ToString("F2")
                        }
                    }
                },
                application_context = new
                {
                    return_url = _settings.ReturnUrl,
                    cancel_url = _settings.CancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders", content);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            var links = doc.RootElement.GetProperty("links");
            foreach (var link in links.EnumerateArray())
            {
                if (link.GetProperty("rel").GetString() == "approve")
                {
                    return link.GetProperty("href").GetString();
                }
            }

            return null;
        }
    }
}

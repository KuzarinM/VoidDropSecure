using Microsoft.Extensions.Options;
using SecretDrop.Configurations;
using SecretDrop.Services.Interfaces;
using System.Text.Json;

namespace SecretDrop.Services.Implementations
{
    public class CaptchaService: ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly AppOptions _options;

        private const string url = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        public CaptchaService(HttpClient httpClient, IOptions<AppOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> VerifyToken(string token)
        {
            if (!_options.UseCaptcha) return true; // Если отключаем капчу

            var response = await _httpClient.PostAsync(
                url,
                new FormUrlEncodedContent(new Dictionary<string, string> {
                    { "secret", _options.CaptchaSecretKey },
                    { "response", token }
                }));

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            return result.RootElement.GetProperty("success").GetBoolean();
        }
    }
}

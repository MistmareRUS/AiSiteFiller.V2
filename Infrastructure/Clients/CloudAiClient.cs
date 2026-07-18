using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Infrastructure.Common.Extensions;

namespace AiSiteFiller.V2.Infrastructure.Clients
{
    public class CloudAiClient : IAiTextClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudAiClient> _logger;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly string _modelName;

        public CloudAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<CloudAiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            _apiUrl = configuration["ApiKeys:ProxyApiUrl"].CleanUrl();
            _modelName = configuration["ApiKeys:ProxyApiModel"] ?? "gpt-4o-mini";

            string rawKey = configuration["ApiKeys:ProxyApiKey"] ?? throw new ArgumentNullException("Ключ ProxyAPI не найден");
            _apiKey = rawKey.CleanToken();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GenerateStructuredPayloadAsync(string productName, string rawCharacteristics, string rawReviews, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Cloud-AI] Запрос к облачному шлюзу {Model} для: {Product}", _modelName, productName);

            var userContent = $"PRODUCT NAME: {productName}\nCHARACTERISTICS:\n{rawCharacteristics}\nREVIEWS:\n{rawReviews}";
            string systemPrompt = Presentation.Program.Configuration["ContentGenerator:SystemPrompt"] ?? "Return JSON.";

            var requestBody = new
            {
                model = _modelName,
                temperature = 0.3,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_apiUrl));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Облачный шлюз вернул ошибку: {response.StatusCode}");

                using var jsonDoc = JsonDocument.Parse(responseString);
                return jsonDoc.RootElement.GetProperty("choices").GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Cloud-AI] Критическая ошибка обмена с облачным ИИ.");
                throw;
            }
        }
    }
}

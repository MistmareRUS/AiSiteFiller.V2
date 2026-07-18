using System;
using System.Net.Http;
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
    public class OllamaTextClient : IAiTextClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaTextClient> _logger;
        private readonly string _apiUrl;
        private readonly string _modelName;

        public OllamaTextClient(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaTextClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            _apiUrl = configuration["ApiKeys:OllamaTextUrl"]?.CleanUrl();
            _modelName = configuration["ApiKeys:OllamaTextModel"] ?? "deepseek-r1:8b";
        }

        public async Task<string> GenerateStructuredPayloadAsync(string productName, string rawCharacteristics, string rawReviews, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Ollama-Text] Запуск локальной модели {Model} для товара: {Product}", _modelName, productName);

            var userContent = $"PRODUCT NAME: {productName}\nCHARACTERISTICS:\n{rawCharacteristics}\nREVIEWS:\n{rawReviews}";
            string systemPrompt = Presentation.Program.Configuration["ContentGenerator:SystemPrompt"] ?? "Return strictly structured JSON format.";

            var requestBody = new
            {
                model = _modelName,
                temperature = 0.3,
                response_format = new { type = "json_object" }, // Поддерживается в современных версиях Ollama
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
                    throw new HttpRequestException($"Локальная Ollama вернула ошибку: {response.StatusCode}");

                using var jsonDoc = JsonDocument.Parse(responseString);
                return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ollama-Text] Критическая ошибка локальной генерации текста.");
                throw;
            }
        }
    }
}

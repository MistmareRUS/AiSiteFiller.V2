using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Infrastructure.Clients
{
    public class DeepSeekClient : IAiTextClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeepSeekClient> _logger;
        private readonly string? _apiUrl;
        private readonly string _apiKey;
        private readonly string _modelName;

        public DeepSeekClient(HttpClient httpClient, IConfiguration configuration, ILogger<DeepSeekClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            // Перенаправляем на переменные рублевого шлюза ProxyAPI
            _apiUrl = configuration["ApiKeys:ProxyApiUrl"]?.CleanUrl();
            _modelName = configuration["ApiKeys:ProxyApiModel"] ?? "gpt-4o-mini";
            _apiKey = configuration["ApiKeys:DeepSeek"] ?? throw new ArgumentNullException("API-ключ шлюза не найден.");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GenerateStructuredPayloadAsync(string productName, string rawCharacteristics, string rawReviews, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Формирование единого аналитического запроса к DeepSeek V3 для товара: {Product}", productName);

            // Собираем пользовательский контекст данных
            var userBuilder = new StringBuilder();
            userBuilder.AppendLine($"PRODUCT NAME: {productName}");
            userBuilder.AppendLine($"CHARACTERISTICS:\n{rawCharacteristics}");
            userBuilder.AppendLine($"REVIEWS FROM CUSTOMERS:\n{rawReviews}");
            string userContent = userBuilder.ToString();

            // Извлекаем системную инструкцию-шаблон из динамического файла prompts.json
            // Наш синглтон конфигурации в Program.cs автоматически подтянет её по ключу
            string systemPrompt = Presentation.Program.Configuration["ContentGenerator:SystemPrompt"]
                ?? "Return strictly structured JSON format with all article content text encoded in Base64.";

            // Строим тело запроса в формате OpenAI/DeepSeek API совместимости
            var requestBody = new
            {
                model = _modelName,
                temperature = 0.3, // Низкая температура для сухой аналитики и защиты от ИИ-воды
                response_format = new { type = "json_object" }, // Активируем режим жесткого JSON на уровне весов сети
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                }
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string jsonPayload = JsonSerializer.Serialize(requestBody, jsonOptions);

            // Используем атомарную сборку URL-строки из паспорта README.AI.md
            string targetUrl = _apiUrl;
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(targetUrl));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                _logger.LogInformation("Отправка HTTP-запроса на эндпоинт DeepSeek...");
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"DeepSeek API вернул ошибку: {response.StatusCode}. Ответ сервера: {responseString}");
                }

                // Парсим полученный конверт ответа, чтобы вытащить чистое тело JSON-пакета
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                string cleanJsonResult = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(cleanJsonResult))
                {
                    throw new InvalidOperationException("Сервер ИИ вернул пустой блок контента в choices[0].message.content.");
                }

                _logger.LogInformation("✅ Структурированный JSON-пакет от DeepSeek V3 успешно получен и валидирован!");
                return cleanJsonResult;
            }
            catch (Exception ex)
            {
                // Жесткое архитектурное правило: пишем ошибку в логи и пробрасываем выше для воркера устойчивости Polly
                _logger.LogError(ex, "Критическая ошибка сетевого обмена с API DeepSeek для товара {Product}.", productName);
                throw;
            }
        }
    }
}

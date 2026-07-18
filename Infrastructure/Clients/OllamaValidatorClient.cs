using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;

namespace AiSiteFiller.V2.Infrastructure.Clients
{
    public class OllamaValidatorClient : IAiValidatorClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaValidatorClient> _logger;
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public OllamaValidatorClient(HttpClient httpClient, ILogger<OllamaValidatorClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }

        public async Task<string> ValidateImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Ollama] Кодирую изображение для отправки локальному цензору Moondream...");

            // Переводим байты в чистую Base64-строку и вычищаем переносы строк под синтаксис Go
            string base64Image = Convert.ToBase64String(imageBytes).Replace("\r", "").Replace("\n", "");

            // Извлекаем промпт-цензор из файла prompts.json (или используем эталонный рабочий промпт)
            string validationPrompt = Presentation.Program.Configuration["ImageValidator:SystemPrompt"]
                ?? "Describe any text, letters, words or distorted objects in this image. If none, write nothing.";

            // Собираем плоский JSON вручную во избежание проблем unmarshal в структурах Go на стороне Ollama
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{\"model\":\"moondream\",\"stream\":false,");
            jsonBuilder.Append($"\"prompt\":\"{validationPrompt}\",");
            jsonBuilder.Append($"\"images\":[\"{base64Image}\"]}}");
            string jsonPayload = jsonBuilder.ToString();

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(OllamaUrl));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Локальный Ollama API вернул ошибку: {response.StatusCode}");
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                string resultText = jsonDoc.RootElement.GetProperty("response").GetString() ?? string.Empty;

                _logger.LogInformation("✅ [Ollama] Вердикт Moondream получен успешно.");
                return resultText.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка сетевого обмена с Ollama по адресу {Url}", OllamaUrl);
                throw;
            }
        }
    }
}

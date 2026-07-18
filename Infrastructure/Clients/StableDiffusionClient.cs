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
    public class StableDiffusionClient : IAiImageClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StableDiffusionClient> _logger;
        private const string SdUrl = "http://localhost:7860/sdapi/v1/txt2img";

        public StableDiffusionClient(HttpClient httpClient, ILogger<StableDiffusionClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }

        public async Task<byte[]> GenerateImageAsync(string englishPrompt, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[GPU] Отправляю промпт на 4070 Super: {Prompt}", englishPrompt);

            // Оптимизируем настройки под Juggernaut Ragnarok на основе вашего рабочего метода
            var imageRequestBody = new
            {
                prompt = "High-end commercial product photography of " + englishPrompt + ", premium studio lighting, hyperrealistic matte textures, sharp focus, cinematic composition, no branding, no text.",
                negative_prompt = "(text:1.4), (words:1.4), (letters:1.4), (font:1.4), (typography:1.4), logo, watermark, signature, blurred, illustration, cartoon",
                steps = 30,
                cfg_scale = 4.5,
                width = 1024,
                height = 576,
                sampler_name = "DPM++ 2M Karras"
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string jsonPayload = JsonSerializer.Serialize(imageRequestBody, jsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(SdUrl));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Локальный Stable Diffusion API вернул ошибку: {response.StatusCode}");
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                var imagesArray = jsonDoc.RootElement.GetProperty("images");

                if (imagesArray.ValueKind == JsonValueKind.Array && imagesArray.GetArrayLength() > 0)
                {
                    string base64Image = imagesArray[0].GetString() ?? string.Empty;
                    if (string.IsNullOrEmpty(base64Image))
                    {
                        throw new InvalidOperationException("Stable Diffusion вернул пустую строку изображения.");
                    }

                    _logger.LogInformation("✅ [GPU] Картинка успешно сгенерирована в байты!");
                    return Convert.FromBase64String(base64Image);
                }

                throw new InvalidOperationException("Структура ответа Stable Diffusion не содержит массив изображений.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка локальной генерации на GPU по адресу {Url}", SdUrl);
                throw;
            }
        }
    }
}

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
    public class WordPressClient : IWordPressClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WordPressClient> _logger;
        private readonly string _baseUrl;

        public WordPressClient(HttpClient httpClient, IConfiguration configuration, ILogger<WordPressClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            // Наш домен: https://tech-info.mistmare.ru
            _baseUrl = configuration["ApiKeys:WordPressUrl"]?.CleanUrl() ?? "https://tech-info.mistmare.ru";
            string wpUser = configuration["ApiKeys:WordPressUser"] ?? "admin";
            string wpAppPassword = configuration["ApiKeys:WordPressAppPassword"] ?? string.Empty;

            // Настраиваем стандартную Basic-авторизацию WordPress REST API
            _httpClient.DefaultRequestHeaders.Clear();
            var authBytes = Encoding.ASCII.GetBytes($"{wpUser}:{wpAppPassword}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }

        public async Task<int> UploadMediaAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[WordPress] Загрузка обложки {FileName} на веб-сервер...", fileName);

            // Атомарная сборка эндпоинта по правилам README.AI.md
            string targetUrl = _baseUrl + "/wp-json/wp/v2/media";

            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");

            // Добавляем файл в multipart-запрос под именем, которое ждет WP
            content.Add(imageContent, "file", fileName);

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(targetUrl)) { Content = content }, cancellationToken);
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ошибка загрузки медиа в WP: {response.StatusCode}. Ответ: {responseString}");
                }

                using var doc = JsonDocument.Parse(responseString);
                int mediaId = doc.RootElement.GetProperty("id").GetInt32();
                _logger.LogInformation("✅ Обложка успешно загружена. WP Media ID: {Id}", mediaId);
                return mediaId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критический сбой при загрузке медиафайла на WordPress.");
                throw;
            }
        }
        public async Task<string> CreatePostAsync(string title, string htmlContent, int featuredMediaId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[WordPress] Публикация текстовой статьи '{Title}'...", title);

            // Атомарная сборка эндпоинта по правилам README.AI.md
            string targetUrl = _baseUrl + "/wp-json/wp/v2/posts";

            // Формируем тело поста в соответствии с документацией WordPress REST API
            var requestBody = new
            {
                title = title,
                content = htmlContent,
                status = "publish", // Сразу публикуем (не в черновики)
                featured_media = featuredMediaId // Привязываем ID загруженной ранее обложки
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(targetUrl));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ошибка создания поста в WP: {response.StatusCode}. Ответ: {responseString}");
                }

                using var jsonDoc = JsonDocument.Parse(responseString);

                // Вытаскиваем финальный публичный URL готовой статьи
                string liveUrl = jsonDoc.RootElement.GetProperty("link").GetString() ?? string.Empty;

                _logger.LogInformation("✅ Статья успешно опубликована на сайте! URL: {Url}", liveUrl);
                return liveUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критический сбой при отправке текстового контента статьи на WordPress.");
                throw;
            }
        }
    }
}

using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Infrastructure.Services
{
    public class TelegramAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramAlertService> _logger;
        private readonly string _bridgeUrl;
        private readonly string _botToken;
        private readonly string _chatId;
        private const string SecretBridgeToken = "MistmareTgBridge2026Secret"; // Жесткий токен авторизации шлюза

        public TelegramAlertService(HttpClient httpClient, IConfiguration configuration, ILogger<TelegramAlertService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            // Считываем адрес PHP-скрипта на Beget (например, https://mysite.ru)
            _bridgeUrl = configuration["ApiKeys:TelegramBridgeUrl"]?.CleanUrl() ?? string.Empty;
            _botToken = configuration["ApiKeys:TelegramBotToken"] ?? string.Empty;
            _chatId = configuration["ApiKeys:TelegramAdminChatId"] ?? string.Empty;
        }

        public async Task SendCriticalAlertAsync(string stageName, Exception exception, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_bridgeUrl) || string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            {
                _logger.LogWarning("⚠️ Telegram-алерты пропущены: в конфигах не настроен TelegramBridgeUrl, BotToken или ChatId.");
                return;
            }

            _logger.LogInformation("[Alert] Формирую аварийный HTML-лог для отправки через Beget-Мост...");

            // Форматируем красивый лог в HTML под правила PHP-скрипта
            var sb = new StringBuilder();
            sb.AppendLine("⚠️ <b>КРИТИЧЕСКИЙ СБОЙ В КОНВЕЙЕРЕ!</b>");
            sb.AppendLine($"• <b>Этап:</b> {stageName}");
            sb.AppendLine($"• <b>Время:</b> {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"• <b>Ошибка:</b> <pre>{exception.Message}</pre>");

            // Наш PHP-скрипт принимает bot_token, chat_id, text и опционально image_base64
            var requestBody = new
            {
                bot_token = _botToken,
                chat_id = _chatId,
                text = sb.ToString(),
                image_base64 = string.Empty // Оставляем пустым для текстовых ошибок
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);

            // Атомарная сборка URL хостинга по правилам паспорта проекта
            string targetUri = _bridgeUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(targetUri));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Защищаем шлюз секретным кастомным заголовком, который проверяет PHP
            request.Headers.Add("X-Bridge-Token", SecretBridgeToken);

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("❌ Beget-Мост отклонил алерт. Код: {Code}, Ответ: {Response}", response.StatusCode, errorContent);
                }
                else
                {
                    _logger.LogInformation("✅ Аварийный алерт успешно проброшен через Beget-Мост в Telegram.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критический сбой отправки данных на хостинг Beget внутри TelegramAlertService.");
            }
        }
    }
}

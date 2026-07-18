using System;
using System.Text;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;

namespace AiSiteFiller.V2.Application.Services
{
    public class Base64DecoderService : IBase64Decoder
    {
        private readonly ILogger<Base64DecoderService> _logger;

        public Base64DecoderService(ILogger<Base64DecoderService> logger)
        {
            _logger = logger;
        }

        public string Decode(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return string.Empty;
            }

            try
            {
                // Стандартное декодирование массива байт в кодировку UTF-8
                byte[] decodedBytes = Convert.FromBase64String(base64String.Trim());
                return Encoding.UTF8.GetString(decodedBytes);
            }
            catch (FormatException ex)
            {
                // Логируем порчу строки ИИ и пробрасываем дальше для Polly-воркера
                _logger.LogError(ex, "Критическая ошибка: Передан невалидный формат Base64-строки для декодирования.");
                throw;
            }
        }
    }
}

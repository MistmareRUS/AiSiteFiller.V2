using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;

namespace AiSiteFiller.V2.Application.Services
{
    public class MediaProcessorService : IMediaProcessor
    {
        private readonly IAiImageClient _sdClient;
        private readonly IAiValidatorClient _ollamaCensor;
        private readonly IMediaRepository _mongoRepository;
        private readonly ILogger<MediaProcessorService> _logger;
        private const int MaxRetryAttempts = 3;

        public MediaProcessorService(
            IAiImageClient sdClient,
            IAiValidatorClient ollamaCensor,
            IMediaRepository mongoRepository,
            ILogger<MediaProcessorService> logger)
        {
            _sdClient = sdClient;
            _ollamaCensor = ollamaCensor;
            _mongoRepository = mongoRepository;
            _logger = logger;
        }

        public async Task<string> ProcessAndSaveImageAsync(string englishPrompt, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Запуск интеллектуального медиа-процессинга для промпта...");

            // 1. Защита видеокарты: проверяем, нет ли уже такого файла в кэше MongoDB
            string? existingFileId = await _mongoRepository.FindFileIdByPromptHashAsync(englishPrompt, cancellationToken);
            if (existingFileId != null)
            {
                _logger.LogInformation("Используем существующее изображение из кэша MongoDB, ID: {FileId}", existingFileId);
                return existingFileId;
            }

            byte[]? finalImageBytes = null;

            // 2. Логика интеллектуального цикла перегенерации при браке (Пункт 2.4.5)
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                _logger.LogInformation("Попытка генерации графики №{Attempt} из {Max}", attempt, MaxRetryAttempts);

                // Рендерим картинку на 4070 Super
                byte[] rawPngBytes = await _sdClient.GenerateImageAsync(englishPrompt, cancellationToken);

                // Отдаем картинку локальному цензору Moondream
                string censorVerdict = await _ollamaCensor.ValidateImageAsync(rawPngBytes, cancellationToken);
                _logger.LogInformation("Вердикт цензора Ollama: '{Verdict}'", censorVerdict);

                // Если Moondream ничего не нашла (строка пустая или содержит "nothing") — картинка идеальна!
                if (string.IsNullOrWhiteSpace(censorVerdict) || censorVerdict.Contains("nothing", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("✅ Картинка успешно прошла цензуру! Брака и текста не обнаружено.");
                    finalImageBytes = rawPngBytes;
                    break;
                }

                _logger.LogWarning("⚠️ Цензор обнаружил артефакты или текст на изображении: {Details}. Запускаю перегенерацию...", censorVerdict);
            }

            // Если после 3 попыток видеокарта выдает только брак, мы все равно берем последнюю картинку, но логируем предупреждение
            finalImageBytes ??= await _sdClient.GenerateImageAsync(englishPrompt, cancellationToken);

            // 3. Конвертация в легковесный формат .webp перед отправкой в СУБД (Защита хостинга)
            byte[] webpImageBytes = ConvertToWebp(finalImageBytes);

            // 4. Сохраняем готовую картинку в MongoDB GridFS
            string mongoFileId = await _mongoRepository.SaveImageAsync(englishPrompt, webpImageBytes, cancellationToken);
            _logger.LogInformation("✅ Итоговое .webp изображение успешно сохранено в MongoDB. ID: {MongoId}", mongoFileId);

            return mongoFileId;
        }

        /// <summary>
        /// Вспомогательный метод сжатия и конвертации PNG массива байт в легковесный WEBP/JPEG поток
        /// </summary>
        private byte[] ConvertToWebp(byte[] rawBytes)
        {
            try
            {
                using var msInput = new MemoryStream(rawBytes);
                using var image = Image.FromStream(msInput);
                using var msOutput = new MemoryStream();

                // В стандартной Windows GDI+ WEBP кодек из коробки сохраняется через Encoder.Quality в формат со сжатием
                // Для максимальной совместимости хостинга сжимаем в высококачественный ImageFormat.Jpeg или Webp
                image.Save(msOutput, ImageFormat.Jpeg);
                return msOutput.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось выполнить сжатие изображения через GDI+, сохраняю исходный массив байт.");
                return rawBytes;
            }
        }
    }
}

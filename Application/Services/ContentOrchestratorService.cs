using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Domain.Entities;
using AiSiteFiller.V2.Domain.Enums;
using AiSiteFiller.V2.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiSiteFiller.V2.Application.Services
{
    public class ContentOrchestratorService : IContentOrchestrator
    {
        private readonly IAiTextClient _deepSeekClient;
        private readonly IMediaProcessor _mediaProcessor;
        private readonly IArticleRepository _articleRepository;
        private readonly IQueueRepository _queueRepository;
        private readonly IBase64Decoder _base64Decoder;
        private readonly ILogger<ContentOrchestratorService> _logger;

        public ContentOrchestratorService(
            IAiTextClient deepSeekClient,
            IMediaProcessor mediaProcessor,
            IArticleRepository articleRepository,
            IQueueRepository queueRepository,
            IBase64Decoder base64Decoder,
            ILogger<ContentOrchestratorService> logger)
        {
            _deepSeekClient = deepSeekClient;
            _mediaProcessor = mediaProcessor;
            _articleRepository = articleRepository;
            _queueRepository = queueRepository;
            _base64Decoder = base64Decoder;
            _logger = logger;
        }

        // Внутренний DTO-класс, полностью повторяющий структуру, которую мы заложили в prompts.json
        private class DeepSeekResponseDto
        {
            public string MetaTags { get; set; } = string.Empty;
            public string SiteTitle { get; set; } = string.Empty;
            public string SiteBody { get; set; } = string.Empty;
            public string VcTitle { get; set; } = string.Empty;
            public string VcBody { get; set; } = string.Empty;
            public string DzenTitle { get; set; } = string.Empty;
            public string DzenBody { get; set; } = string.Empty;
            public string GraphicPrompt { get; set; } = string.Empty;
        }
        public async Task<long> GenerateAndQueueArticleAsync(string productName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== СТАРТ ПОЛНОГО ЦИКЛА КОНВЕЙЕРА ДЛЯ: {Product} ===", productName);

            try
            {
                // 1. Делаем единый запрос к текстовому ИИ с использованием правильного await
                string rawJsonResult = await _deepSeekClient.GenerateStructuredPayloadAsync(productName, "Характеристики", "Отзывы", cancellationToken);

                // Очищаем и передаем cleanJson дальше — и в сериализатор, и в базу данных SQL
                string cleanJson = rawJsonResult.CleanJsonPayload();

                // 2. Десериализуем очищенный конверт ответа
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var dto = JsonSerializer.Deserialize<DeepSeekResponseDto>(cleanJson, jsonOptions)
                    ?? throw new InvalidOperationException("Не удалось десериализовать JSON-пакет от ИИ.");


                // 3. Отправляем англоязычный промпт в графический контур (Stable Diffusion + Ollama)
                string mongoFileId = await _mediaProcessor.ProcessAndSaveImageAsync(dto.GraphicPrompt, cancellationToken);

                // 4. Сборка сущности Article с декодированием Base64 текстовых блоков
                var article = new Article
                {
                    ProductName = productName,
                    Language = LangType.RU, // По умолчанию RU контур
                    RawJsonResponse = cleanJson,
                    SiteTitle = dto.SiteTitle.Trim(),
                    SiteBodyBase64 = _base64Decoder.Decode(dto.SiteBody),
                    VcTitle = dto.VcTitle.Trim(),
                    VcBodyBase64 = _base64Decoder.Decode(dto.VcBody),
                    DzenTitle = dto.DzenTitle.Trim(),
                    DzenBodyBase64 = _base64Decoder.Decode(dto.DzenBody),
                    CreatedAt = DateTime.UtcNow
                };

                // Привязываем ID сгенерированной обложки в наш гибкий словарь метаданных
                article.ImagesMetadata.Add("MainImage", mongoFileId);

                // 5. Транзакционное сохранение статьи в PostgreSQL для получения её ArticleId
                long articleId = await _articleRepository.AddAsync(article, cancellationToken);
                _logger.LogInformation("Статья успешно сохранена в Postgres. Присвоен ID: {ArticleId}", articleId);

                // 6. Автоматическая нарезка задач в очередь публикаций для платформ "веера"
                var tasksToQueue = new List<QueueTask>
                {
                    // Наш собственный сайт (ядро прибыли) — публикуется в первую очередь
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.WordPress, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow },
                    
                    // Внешние платформы дистрибуции (анонсы) — планируем со случайным шагом наперед (например, через 2, 4 часа)
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.VCRu, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow.AddHours(2) },
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.Dzen, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow.AddHours(4) }
                };

                await _queueRepository.AddRangeAsync(tasksToQueue, cancellationToken);
                _logger.LogInformation("✅ Все задачи веера для статьи {ArticleId} успешно поставлены в очередь!", articleId);

                return articleId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ КРИТИЧЕСКИЙ СБОЙ КОНВЕЙЕРА ДЛЯ ТОВАРА: {Product}.", productName);
                throw;
            }
        }
    }
}

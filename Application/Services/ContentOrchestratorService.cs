using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Domain.Entities;
using AiSiteFiller.V2.Domain.Enums;
using AiSiteFiller.V2.Infrastructure.Common.Extensions;

namespace AiSiteFiller.V2.Application.Services
{
    public class ContentOrchestratorService : IContentOrchestrator
    {
        private readonly IAiTextClient _cloudAiClient;
        private readonly IMediaProcessor _mediaProcessor;
        private readonly IArticleRepository _articleRepository;
        private readonly IQueueRepository _queueRepository;
        private readonly IProductParser _productParser; // 🔥 Наш новый автоматический сборщик
        private readonly ILogger<ContentOrchestratorService> _logger;

        public ContentOrchestratorService(
            IAiTextClient cloudAiClient,
            IMediaProcessor mediaProcessor,
            IArticleRepository articleRepository,
            IQueueRepository queueRepository,
            IProductParser productParser, // Внедряем через DI
            ILogger<ContentOrchestratorService> logger)
        {
            _cloudAiClient = cloudAiClient;
            _mediaProcessor = mediaProcessor;
            _articleRepository = articleRepository;
            _queueRepository = queueRepository;
            _productParser = productParser;
            _logger = logger;
        }

        public async Task<long> GenerateAndQueueArticleAsync(string productName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== СТАРТ ПОЛНОГО ЦИКЛА КОНВЕЙЕРА ДЛЯ: {Product} ===", productName);

            try
            {
                // 1. АВТОМАТИЧЕСКИЙ СБОР ДАННЫХ ИЗ ИНТЕРНЕТА (Пункт 4.2)
                // Выкачиваем сухую правду и реальный опыт людей по названию модели
                var parsedData = await _productParser.ParseProductDataAsync(productName, cancellationToken);

                // 2. ОТПРАВКА СВЕЖИХ ДАННЫХ В ОБЛАКО PROXYAPI (gpt-4o-mini)
                // Передаем реальные строки вместо прошлых текстовых заглушек
                string rawJsonResult = await _cloudAiClient.GenerateStructuredPayloadAsync(
                    productName,
                    parsedData.Characteristics,
                    parsedData.Reviews,
                    cancellationToken);

                string cleanJson = rawJsonResult.CleanJsonPayload();

                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var dto = JsonSerializer.Deserialize<CloudAiResponseDto>(cleanJson, jsonOptions)
                    ?? throw new InvalidOperationException("Не удалось десериализовать JSON-пакет от ИИ.");

                // 3. Генерация и кэширование обложки на GPU (Stable Diffusion + Ollama)
                string mongoFileId = await _mediaProcessor.ProcessAndSaveImageAsync(dto.GraphicPrompt, cancellationToken);

                // 4. Сборка сущности и сохранение в Postgres
                var article = new Article
                {
                    ProductName = productName,
                    Language = LangType.RU,
                    RawJsonResponse = cleanJson,
                    SiteTitle = dto.SiteTitle.Trim(),
                    SiteBodyBase64 = dto.SiteBody.Trim(),
                    VcTitle = dto.SiteTitle.Trim(),
                    VcBodyBase64 = dto.SiteBody.Trim(),
                    DzenTitle = dto.SiteTitle.Trim(),
                    DzenBodyBase64 = dto.SiteBody.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                article.ImagesMetadata.Add("MainImage", mongoFileId);

                long articleId = await _articleRepository.AddAsync(article, cancellationToken);
                _logger.LogInformation("Статья успешно сохранена в Postgres. Присвоен ID: {Id}", articleId);

                // 5. Нарезка веера публикаций в очередь СУБД
                var tasksToQueue = new List<QueueTask>
                {
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.WordPress, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow },
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.VCRu, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow.AddHours(2) },
                    new QueueTask { ArticleId = articleId, Platform = PlatformType.Dzen, Status = PublicationStatus.Pending, ScheduledAt = DateTime.UtcNow.AddHours(4) }
                };

                await _queueRepository.AddRangeAsync(tasksToQueue, cancellationToken);
                _logger.LogInformation("✅ Все задачи веера для статьи {Id} успешно поставлены в очередь!", articleId);

                return articleId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ КРИТИЧЕСКИЙ СБОЙ КОНВЕЙЕРА ДЛЯ ТОВАРА: {Product}.", productName);
                throw;
            }
        }

        private class CloudAiResponseDto
        {
            public string SiteTitle { get; set; } = string.Empty;
            public string SiteBody { get; set; } = string.Empty;
            public string GraphicPrompt { get; set; } = string.Empty;
        }
    }
}

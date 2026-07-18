using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Domain.Entities;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Infrastructure.BackgroundServices
{
    public class QueueProcessorWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueueProcessorWorker> _logger;
        private readonly IAlertService _alertService;

        public QueueProcessorWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<QueueProcessorWorker> logger,
            IAlertService alertService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _alertService = alertService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("=== ФОНОВЫЙ ВОРКЕР ОЧЕРЕДИ ПУБЛИКАЦИЙ ЗАПУЩЕН ===");

            // Бесконечный цикл, пока фоновая служба не будет остановлена операционной системой
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Вызываем основной метод обработки одной задачи
                    await ProcessNextQueueTaskAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критический неотловленный сбой в цикле планировщика очереди.");
                }

                // Интервал опроса базы данных — 5 минут (300 000 миллисекунд)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        private async Task ProcessNextQueueTaskAsync(CancellationToken cancellationToken)
        {
            // Создаем изолированный Scope для безопасного извлечения Scoped-репозиториев данных
            using var scope = _scopeFactory.CreateScope();

            var queueRepo = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
            var articleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
            var wpClient = scope.ServiceProvider.GetRequiredService<IWordPressClient>();
            var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

            // 1. Запрашиваем у репозитория следующую созревшую задачу
            var task = await queueRepo.GetNextPendingTaskAsync(cancellationToken);
            if (task == null)
            {
                return; // Очередь пуста, спокойно ждем следующего тика таймера
            }

            _logger.LogInformation("[Worker] Найдена созревшая задача №{TaskId} для платформы {Platform}.", task.QueueTaskId, task.Platform);

            // 2. Извлекаем полную сущность статьи из PostgreSQL
            var article = await articleRepo.GetByIdAsync(task.ArticleId, cancellationToken);
            if (article == null)
            {
                _logger.LogError("[Worker] Критическая ошибка: Статья с ID {ArticleId} не найдена в базе данных.", task.ArticleId);
                task.Status = PublicationStatus.Error;
                task.LastError = "Статья удалена из репозитория Articles.";
                await queueRepo.UpdateTaskAsync(task, cancellationToken);
                return;
            }

            try
            {
                // Маршрутизируем задачу в зависимости от целевой платформы "веера"
                if (task.Platform == PlatformType.WordPress)
                {
                    // Вытаскиваем ID обложки из метаданных статьи
                    if (!article.ImagesMetadata.TryGetValue("MainImage", out var mongoFileId))
                    {
                        throw new InvalidOperationException("В метаданных статьи отсутствует ссылка MainImage на MongoDB.");
                    }

                    // Скачиваем сырые байты обложки из NoSQL MongoDB GridFS
                    byte[] imageBytes = await mediaRepo.GetImageBytesAsync(mongoFileId, cancellationToken);

                    // Сначала заливаем картинку на сайт WordPress
                    string fileName = $"cover_{article.ArticleId}.jpg";
                    int wpMediaId = await wpClient.UploadMediaAsync(imageBytes, fileName, cancellationToken);

                    // Публикуем текстовое тело статьи (чистый HTML) с привязкой обложки
                    string liveUrl = await wpClient.CreatePostAsync(article.SiteTitle, article.SiteBodyBase64, wpMediaId, cancellationToken);

                    // Фиксируем успех в базе данных
                    task.Status = PublicationStatus.Posted;
                    task.LiveUrl = liveUrl;
                    task.ProcessedAt = DateTime.UtcNow;
                    await queueRepo.UpdateTaskAsync(task, cancellationToken);

                    _logger.LogInformation("✅ [Worker] Задача №{TaskId} успешно выполнена! Статья доступна по адресу: {Url}", task.QueueTaskId, liveUrl);
                }
                else
                {
                    // Временная заглушка для внешних платформ веера (VC, Дзен), которые мы будем кодить на Этапе 5
                    _logger.LogInformation("[Worker] Публикация на внешнюю платформу {Platform} временно пропущена (ожидает Этапа 5).", task.Platform);
                    task.Status = PublicationStatus.Posted;
                    task.ProcessedAt = DateTime.UtcNow;
                    await queueRepo.UpdateTaskAsync(task, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // РЕАЛИЗАЦИЯ ПУНКТА 2.5.3: Если все попытки Polly исчерпаны и вызов упал — фиксируем Error в Postgres
                _logger.LogError(ex, "❌ [Worker] Ошибка публикации задачи №{TaskId}. Фиксирую сбой в PostgreSQL.", task.QueueTaskId);

                task.Status = PublicationStatus.Error;
                task.RetryCount += 1;
                task.LastError = ex.Message;
                task.ProcessedAt = DateTime.UtcNow;

                await queueRepo.UpdateTaskAsync(task, cancellationToken);

                // Отправляем детальный лог аварийного завершения вам в Telegram-личку через Beget-мост
                await _alertService.SendCriticalAlertAsync($"Воркер Очереди - {task.Platform}", ex, cancellationToken);
            }
        }
    }
}

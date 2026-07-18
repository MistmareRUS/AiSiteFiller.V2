using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Domain.Entities;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Infrastructure.Persistence
{
    public class EfRepository : IArticleRepository, IQueueRepository, ITitleLogRepository
    {
        private readonly AppDbContext _context;

        public EfRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region IArticleRepository Реализация

        public async Task<long> AddAsync(Article article, CancellationToken cancellationToken = default)
        {
            await _context.Articles.AddAsync(article, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return article.ArticleId;
        }

        public async Task<Article?> GetByIdAsync(long articleId, CancellationToken cancellationToken = default)
        {
            return await _context.Articles
                .FirstOrDefaultAsync(a => a.ArticleId == articleId, cancellationToken);
        }

        public async Task UpdateAsync(Article article, CancellationToken cancellationToken = default)
        {
            _context.Articles.Update(article);
            await _context.SaveChangesAsync(cancellationToken);
        }

        #endregion
        #region IQueueRepository Реализация

        public async Task AddRangeAsync(IEnumerable<QueueTask> tasks, CancellationToken cancellationToken = default)
        {
            await _context.PublishingQueue.AddRangeAsync(tasks, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<QueueTask?> GetNextPendingTaskAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PublishingQueue
                .Where(t => t.Status == PublicationStatus.Pending && t.ScheduledAt <= DateTime.UtcNow)
                .OrderBy(t => t.QueueTaskId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateTaskAsync(QueueTask task, CancellationToken cancellationToken = default)
        {
            _context.PublishingQueue.Update(task);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ResetFailedTasksAsync(CancellationToken cancellationToken = default)
        {
            var failedTasks = await _context.PublishingQueue
                .Where(t => t.Status == PublicationStatus.Error)
                .ToListAsync(cancellationToken);

            foreach (var task in failedTasks)
            {
                task.Status = PublicationStatus.Pending;
                task.RetryCount = 0;
                task.LastError = null;
            }

            if (failedTasks.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion

        #region ITitleLogRepository Реализация

        public async Task LogTitleAsync(PlatformType platform, string titleText, CancellationToken cancellationToken = default)
        {
            var log = new TitleLog
            {
                Platform = platform,
                TitleText = titleText,
                UsedAt = DateTime.UtcNow
            };

            await _context.UsedTitlesLog.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<string>> GetLatestTitlesAsync(PlatformType platform, int limit = 30, CancellationToken cancellationToken = default)
        {
            return await _context.UsedTitlesLog
                .Where(l => l.Platform == platform)
                .OrderByDescending(l => l.UsedAt)
                .Select(l => l.TitleText)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        #endregion
    }
}

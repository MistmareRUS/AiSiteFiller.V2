using System.Threading;
using System.Threading.Tasks;
using AiSiteFiller.V2.Domain.Entities;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IArticleRepository
    {
        /// <summary>
        /// Сохраняет сгенерированную ИИ статью в базу данных и возвращает её сгенерированный ID
        /// </summary>
        Task<long> AddAsync(Article article, CancellationToken cancellationToken = default);

        /// <summary>
        /// Извлекает полную информацию о статье по её уникальному идентификатору
        /// </summary>
        Task<Article?> GetByIdAsync(long articleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет метаданные изображений или измененный контент статьи
        /// </summary>
        Task UpdateAsync(Article article, CancellationToken cancellationToken = default);
    }
}

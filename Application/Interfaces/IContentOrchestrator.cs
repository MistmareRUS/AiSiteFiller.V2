using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IContentOrchestrator
    {
        /// <summary>
        /// Запускает полный цикл фабрики для одного товара: анализ DeepSeek -> генерация на GPU -> валидация Ollama -> сохранение в Postgres и создание очереди.
        /// </summary>
        Task<long> GenerateAndQueueArticleAsync(string productName, CancellationToken cancellationToken = default);
    }
}

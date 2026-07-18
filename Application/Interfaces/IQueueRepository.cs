using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiSiteFiller.V2.Domain.Entities;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IQueueRepository
    {
        /// <summary>
        /// Добавляет пачку созданных задач в очередь для конкретной статьи
        /// </summary>
        Task AddRangeAsync(IEnumerable<QueueTask> tasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Извлекает ОДНУ следующую задачу, готовую к публикации (поштучный выкат по расписанию)
        /// </summary>
        Task<QueueTask?> GetNextPendingTaskAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет статус, счетчик попыток и логи ошибок Polly для текущей задачи в очереди
        /// </summary>
        Task UpdateTaskAsync(QueueTask task, CancellationToken cancellationToken = default);

        /// <summary>
        /// Опциональный сброс: переводит все упавшие в Error задачи обратно в Pending на старте отладки
        /// </summary>
        Task ResetFailedTasksAsync(CancellationToken cancellationToken = default);
    }
}

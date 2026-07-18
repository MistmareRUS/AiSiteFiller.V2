using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface ITitleLogRepository
    {
        /// <summary>
        /// Фиксирует использованный заголовок в логе при успешном постинге
        /// </summary>
        Task LogTitleAsync(PlatformType platform, string titleText, CancellationToken cancellationToken = default);

        /// <summary>
        /// Выгружает список последних 30 заголовков для конкретной платформы (для передачи ИИ в качестве стоп-контекста)
        /// </summary>
        Task<List<string>> GetLatestTitlesAsync(PlatformType platform, int limit = 30, CancellationToken cancellationToken = default);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IAlertService
    {
        /// <summary>
        /// Отправляет экстренное форматированное сообщение о сбое в ваш Telegram-канал/чат.
        /// </summary>
        /// <param name="stageName">Название этапа, где произошел сбой (например, "DeepSeek API")</param>
        /// <param name="exception">Исключение с полным стеком ошибки</param>
        Task SendCriticalAlertAsync(string stageName, Exception exception, CancellationToken cancellationToken = default);
    }
}

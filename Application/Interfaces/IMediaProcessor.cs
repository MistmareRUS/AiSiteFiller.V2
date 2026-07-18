using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IMediaProcessor
    {
        /// <summary>
        /// Запускает цикл генерации обложки, проверяет её через Ollama и сохраняет чистый .webp результат в MongoDB.
        /// Возвращает строковый ID сохраненного файла (ObjectId).
        /// </summary>
        Task<string> ProcessAndSaveImageAsync(string englishPrompt, CancellationToken cancellationToken = default);
    }
}

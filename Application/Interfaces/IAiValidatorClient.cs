using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IAiValidatorClient
    {
        /// <summary>
        /// Отправляет массив байт картинки в локальную Ollama (Moondream).
        /// Возвращает текстовое описание дефектов, текста или содержимого на изображении.
        /// </summary>
        Task<string> ValidateImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
    }
}

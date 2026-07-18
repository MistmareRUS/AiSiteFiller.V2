using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IAiImageClient
    {
        /// <summary>
        /// Отправляет англоязычный промпт в локальный Stable Diffusion API.
        /// Возвращает сырой массив байт сгенерированного изображения (.png/.webp).
        /// </summary>
        Task<byte[]> GenerateImageAsync(string englishPrompt, CancellationToken cancellationToken = default);
    }
}

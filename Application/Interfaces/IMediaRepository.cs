using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IMediaRepository
    {
        /// <summary>
        /// Проверяет, была ли уже сгенерирована картинка по такому текстовому промпту.
        /// Если да — возвращает строковый ID существующего файла в MongoDB (ObjectId). Если нет — null.
        /// </summary>
        Task<string?> FindFileIdByPromptHashAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет сырой массив байт изображения в MongoDB GridFS, привязывая его к хэшу промпта.
        /// Возвращает сгенерированный строковый ID файла (ObjectId).
        /// </summary>
        Task<string> SaveImageAsync(string prompt, byte[] imageBytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Извлекает сырой массив байт изображения из MongoDB GridFS по его строковому идентификатору.
        /// </summary>
        Task<byte[]> GetImageBytesAsync(string fileId, CancellationToken cancellationToken = default);
    }
}

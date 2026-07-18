using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IWordPressClient
    {
        /// <summary>
        /// Публикует обложку статьи (.webp байты) на сайт WordPress через REST API.
        /// Возвращает ID загруженного медиафайла в системе WordPress (целое число).
        /// </summary>
        Task<int> UploadMediaAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Публикует готовую текстовую статью на сайт WordPress и привязывает к ней обложку.
        /// Возвращает итоговый публичный URL опубликованной статьи.
        /// </summary>
        Task<string> CreatePostAsync(string title, string htmlContent, int featuredMediaId, CancellationToken cancellationToken = default);
    }
}

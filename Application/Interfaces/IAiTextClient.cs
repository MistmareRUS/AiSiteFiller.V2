using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IAiTextClient
    {
        /// <summary>
        /// Отправляет сырые характеристики и отзывы товара в DeepSeek V3.
        /// Возвращает чистую строку структурированного JSON-пакета в формате Base64.
        /// </summary>
        /// <param name="productName">Название модели устройства</param>
        /// <param name="rawCharacteristics">Сухие технические параметры</param>
        /// <param name="rawReviews">Сводка реальных отзывов покупателей</param>
        Task<string> GenerateStructuredPayloadAsync(string productName, string rawCharacteristics, string rawReviews, CancellationToken cancellationToken = default);
    }
}

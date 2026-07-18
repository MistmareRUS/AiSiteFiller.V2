using System.Threading;
using System.Threading.Tasks;

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IProductParser
    {
        /// <summary>
        /// Результат автоматического сбора данных по модели товара
        /// </summary>
        public record ParserResult(string Characteristics, string Reviews);

        /// <summary>
        /// Парсит маркетплейсы по названию товара и возвращает агрегированные технические параметры и пачку реальных отзывов покупателей.
        /// </summary>
        Task<ParserResult> ParseProductDataAsync(string productName, CancellationToken cancellationToken = default);
    }
}

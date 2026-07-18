using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Domain.Entities
{
    public class Article
    {
        public long ArticleId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public LangType Language { get; set; } = LangType.RU;
        public string RawJsonResponse { get; set; } = string.Empty;

        // Текстовые Base64-блоки контента
        public string SiteTitle { get; set; } = string.Empty;
        public string SiteBodyBase64 { get; set; } = string.Empty;
        public string VcTitle { get; set; } = string.Empty;
        public string VcBodyBase64 { get; set; } = string.Empty;
        public string DzenTitle { get; set; } = string.Empty;
        public string DzenBodyBase64 { get; set; } = string.Empty;

        // Словарь картинок: Ключ (место на сайте) -> Значение (ID файла в MongoDB ObjectId)
        // Пример: ["MainImage"] = "64b15c...", ["ConsImage"] = "64b15d..."
        public Dictionary<string, string> ImagesMetadata { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

namespace AiSiteFiller.V2.Application.Interfaces
{
    public interface IBase64Decoder
    {
        /// <summary>
        /// Безопасно переводит строку из формата Base64 в обычный читаемый UTF-8 текст.
        /// Если строка пустая, возвращает пустую строку.
        /// </summary>
        /// <param name="base64String">Замаскированная ИИ строка контента</param>
        string Decode(string base64String);
    }
}

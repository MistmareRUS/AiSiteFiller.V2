using System;

namespace AiSiteFiller.V2.Infrastructure.Common.Extensions
{
    public static class StringExtensions
    {
        public static string CleanUrl(this string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            return url.Replace(" + ", "");
        }

        /// <summary>
        /// Вырезает из ответа ИИ блоки <think></think> и маркеры кода ```json, 
        /// гарантируя получение чистого JSON-пакета для десериализации.
        /// </summary>
        public static string CleanJsonPayload(this string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            string result = json.Trim();

            // 1. Вырезаем блок размышлений модели DeepSeek-R1, если он присутствует
            if (result.Contains("<think>") && result.Contains("</think>"))
            {
                int startIndex = result.IndexOf("<think>");
                int endIndex = result.IndexOf("</think>") + "</think>".Length;
                result = result.Remove(startIndex, endIndex - startIndex).Trim();
            }

            // 2. Срезаем markdown-обертки кода
            if (result.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                result = result.Substring(7).Trim();
            else if (result.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                result = result.Substring(3).Trim();

            if (result.EndsWith("```"))
                result = result.Substring(0, result.Length - 3).Trim();

            return result;
        }
    }
}

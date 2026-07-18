using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;

namespace AiSiteFiller.V2.Infrastructure.Services
{
    public class UiLogSink : ILogEventSink
    {
        // Статическое событие, на которое подпишется наша WinForms "морда"
        public static event Action<string>? OnLogReceived;

        private readonly IFormatProvider? _formatProvider;

        public UiLogSink(IFormatProvider? formatProvider = null)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            // Форматируем лог в красивую строку: [Время] [Уровень] Сообщение
            var message = $"[{logEvent.Timestamp:HH:mm:ss}] [{logEvent.Level.ToString().Substring(0, 3).ToUpper()}] {logEvent.RenderMessage(_formatProvider)}";

            // Запускаем событие, передавая строку в UI поток
            OnLogReceived?.Invoke(message);
        }
    }
}

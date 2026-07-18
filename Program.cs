using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AiSiteFiller.V2.Presentation.Common;

namespace AiSiteFiller.V2.Presentation
{
    internal static class Program
    {
        public static IConfiguration Configuration { get; private set; } = null!;
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        [STAThread]
        private static void Main()
        {
            try
            {
                // Инициализируем сборщик конфигурации (Пункты 2.2.2 - 2.2.3)
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("prompts.json", optional: true, reloadOnChange: true);

                Configuration = builder.Build();

                // Инициализируем пул зависимостей DI-контейнера (Пункт 2.2.4)
                var services = new ServiceCollection();

                // Вызываем наш метод расширения для наполнения пула базовыми службами
                services.AddPresentationServices(Configuration);

                // Строим провайдер служб, из которого WinForms будет извлекать готовые объекты
                ServiceProvider = services.BuildServiceProvider();

                MessageBox.Show("Пул зависимостей DI-контейнера на .NET 10 успешно собран!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Жесткое правило: логируем критическую ошибку и пробрасываем её дальше
                MessageBox.Show($"Критический сбой инициализации пула DI: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
}

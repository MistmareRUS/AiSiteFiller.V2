using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using AiSiteFiller.V2.Presentation.Common;
using AiSiteFiller.V2.Infrastructure.Persistence;

namespace AiSiteFiller.V2.Presentation
{
    public static class Program
    {
        private static IConfiguration? _configuration;
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static IConfiguration Configuration => _configuration ??= new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddJsonFile("prompts.json", optional: true, reloadOnChange: true)
            .Build();

        // === МЕТКА: БЕЗОПАСНЫЙ СТАРТ WINFORMS (БЕЗ ФОНОВЫХ УТЕЧЕК) ===
        [STAThread]
        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Sink(new Infrastructure.Services.UiLogSink())
                .CreateLogger();

            try
            {
                Log.Information("=== ЗАПУСК ФАБРИКИ AISITEFILLER.V2 (WINFORMS) ===");

                ApplicationConfiguration.Initialize();

                var services = new ServiceCollection();
                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                services.AddPresentationServices(Configuration);

                // Регистрируем форму в контейнере зависимостей
                services.AddTransient<MainForm>();

                ServiceProvider = services.BuildServiceProvider();

                // Запускаем графический цикл — теперь рантайм не упадет на старте
                var mainForm = ServiceProvider.GetRequiredService<MainForm>();
                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критический сбой при инициализации приложения.");
                MessageBox.Show($"Критический сбой инициализации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

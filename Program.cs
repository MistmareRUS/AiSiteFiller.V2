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
using AiSiteFiller.V2.Application.Interfaces;

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

        [STAThread]
        private static async Task Main()
        {
            // Инициализация Serilog (логи на диск и в будущее графическое окно)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("=== СТАРТ ЛОКАЛЬНОГО ИНТЕГРАЦИОННОГО КОНВЕЙЕРА ===");

                var services = new ServiceCollection();
                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                services.AddPresentationServices(Configuration);
                ServiceProvider = services.BuildServiceProvider();

                // Автоматическое применение миграций к PostgreSQL при каждом запуске приложения
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();
                    Log.Information("База данных PostgreSQL успешно синхронизирована с миграциями EF Core.");

                    // Логика автоматического ремонта зафейленных задач на этапе отладки
                    bool shouldReset = Configuration.GetValue<bool>("AppSettings:ResetErrorsOnStart");
                    if (shouldReset)
                    {
                        var queueRepo = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
                        await queueRepo.ResetFailedTasksAsync();
                    }
                }

                // ВЫЗОВ ОРКЕСТРАТОРА (ТЕСТОВАЯ ПЕРВАЯ ГЕНЕРАЦИЯ БЕЗ UI)
                using (var scope = ServiceProvider.CreateScope())
                {
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IContentOrchestrator>();

                    // Задаем тестовую трендовую модель для проверки всех слоев архитектуры
                    string testProduct = "Roborock S8 MaxV Ultra Black";

                    long articleId = await orchestrator.GenerateAndQueueArticleAsync(testProduct);
                    Log.Information("=== УСПЕХ! Статья {Id} сгенерирована и нарезана в очередь публикаций. ===", articleId);
                }

                MessageBox.Show("Первый тестовый запуск конвейера успешно завершен! Базы данных наполнены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критический сбой в главном потоке фабрики контента.");
                MessageBox.Show($"Критический сбой конвейера: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}

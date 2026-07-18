using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Application.Services;
using AiSiteFiller.V2.Infrastructure.BackgroundServices;
using AiSiteFiller.V2.Infrastructure.Clients;
using AiSiteFiller.V2.Infrastructure.Persistence;
using AiSiteFiller.V2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using System;
using System.Net.Http;

namespace AiSiteFiller.V2.Presentation.Common
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);

            // 1. Настройка реляционной базы данных PostgreSQL (EF Core)
            var connectionString = configuration.GetConnectionString("Postgres");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 2. Регистрация репозиториев PostgreSQL
            services.AddScoped<EfRepository>();
            services.AddTransient<IArticleRepository>(sp => sp.GetRequiredService<EfRepository>());
            services.AddTransient<IQueueRepository>(sp => sp.GetRequiredService<EfRepository>());
            services.AddTransient<ITitleLogRepository>(sp => sp.GetRequiredService<EfRepository>());

            // 3. Регистрация медиа-репозитория NoSQL MongoDB
            services.AddSingleton<IMediaRepository, MongoMediaRepository>();

            // 4. Регистрация утилит и сервисов слоя Application
            services.AddTransient<IBase64Decoder, Base64DecoderService>();
            services.AddTransient<IMediaProcessor, MediaProcessorService>();
            services.AddTransient<IContentOrchestrator, ContentOrchestratorService>();

            // 5. НАСТРОЙКА КОРНЕВОЙ ПОЛИТИКИ УСТОЙЧИВОСТИ POLLY V8 ЧЕРЕЗ РОДНОЙ HTTP-ХЭНДЛЕР .NET 10
            var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(response => (int)response.StatusCode >= 500)
            };

            // 6. Регистрация HttpClient-клиентов ИИ с привязкой хэндлера устойчивости Polly v8
            services.AddHttpClient<CloudAiClient>().AddResilienceHandler("ProxyApiPipeline", pb => pb.AddRetry(retryOptions));

            // Задаем локальному текстовому ИИ таймаут в 5 минут, чтобы он успевал продумать и выдать весь Base64-JSON
            services.AddHttpClient<OllamaTextClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(8);
            }).AddResilienceHandler("OllamaTextPipeline", pb => pb.AddRetry(retryOptions));


            // ДИНАМИЧЕСКАЯ ФАБРИКА СЕЛЕКТОРА ДВИЖКА ИИ (Пункт 4.1.2)
            services.AddTransient<IAiTextClient>(sp =>
            {
                bool useLocal = configuration.GetValue<bool>("AppSettings:UseLocalAi");
                return useLocal
                    ? (IAiTextClient)sp.GetRequiredService<OllamaTextClient>()
                    : (IAiTextClient)sp.GetRequiredService<CloudAiClient>();
            });

            // Графика и цензор
            services.AddHttpClient<IAiImageClient, StableDiffusionClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(3);
            }).AddResilienceHandler("StableDiffusionPipeline", pb => pb.AddRetry(retryOptions));

            services.AddHttpClient<IAiValidatorClient, OllamaValidatorClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(1);
            }).AddResilienceHandler("OllamaPipeline", pb => pb.AddRetry(retryOptions));

            // Аварийные оповещения
            services.AddHttpClient<IAlertService, TelegramAlertService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // WordPress REST API
            services.AddHttpClient<IWordPressClient, WordPressClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            }).AddResilienceHandler("WpPipeline", pb => pb.AddRetry(retryOptions));
            
            // 9. Регистрация HttpClient-парсера контента с защитой Polly v8
            services.AddHttpClient<IProductParser, MarketProductParser>()
                .AddResilienceHandler("ParserPipeline", pb => pb.AddRetry(retryOptions));

            // 10. Регистрация долгоживущей фоновой службы автоматического выката статей по расписанию
            services.AddHostedService<QueueProcessorWorker>();



            return services;
        }
    }
}

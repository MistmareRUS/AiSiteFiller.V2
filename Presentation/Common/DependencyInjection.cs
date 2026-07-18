using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiSiteFiller.V2.Presentation.Common
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Метод расширения для централизованной регистрации всех локальных служб фабрики в пул
        /// </summary>
        public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Регистрируем сам объект конфигурации в DI, чтобы его могли запрашивать другие сервисы
            services.AddSingleton<IConfiguration>(configuration);

            // 2. Сюда мы будем последовательно добавлять репозитории баз данных и ИИ-клиенты по ходу выполнения плана

            return services;
        }
    }
}

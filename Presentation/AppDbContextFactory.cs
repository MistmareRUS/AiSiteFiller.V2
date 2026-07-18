using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using AiSiteFiller.V2.Presentation;

namespace AiSiteFiller.V2.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Обращение к свойству автоматически соберет конфигурацию в Program.cs
            var connectionString = Program.Configuration.GetConnectionString("Postgres");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Критическая ошибка: Строка подключения к Postgres отсутствует в конфигурационных файлах.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;
using AiSiteFiller.V2.Domain.Entities;

namespace AiSiteFiller.V2.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<QueueTask> PublishingQueue => Set<QueueTask>();
        public DbSet<TitleLog> UsedTitlesLog => Set<TitleLog>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настраиваем маппинг словаря картинок Dictionary<string, string> в формат JSON для PostgreSQL
            modelBuilder.Entity<Article>()
                .Property(a => a.ImagesMetadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, string>()
                );

            // Гарантируем, что EF Core не будет принудительно переводить имена в snake_case
            modelBuilder.Entity<Article>().ToTable("Articles");
            modelBuilder.Entity<QueueTask>().ToTable("PublishingQueue");
            modelBuilder.Entity<TitleLog>().ToTable("UsedTitlesLog");
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using AiSiteFiller.V2.Application.Interfaces;

namespace AiSiteFiller.V2.Infrastructure.Persistence
{
    public class MongoMediaRepository : IMediaRepository
    {
        private readonly IMongoDatabase _database;
        private readonly IGridFSBucket _bucket;
        private readonly ILogger<MongoMediaRepository> _logger;

        public MongoMediaRepository(IConfiguration configuration, ILogger<MongoMediaRepository> logger)
        {
            _logger = logger;
            var connectionString = configuration.GetConnectionString("Mongo")
                ?? throw new ArgumentNullException("Строка подключения к MongoDB не найдена в конфигурации.");

            var client = new MongoClient(connectionString);

            // База данных медиа-контура, как зафиксировано в README.AI.md
            _database = client.GetDatabase("AiSiteFillerMedia");

            // Инициализируем ведро GridFS для хранения бинарных файлов (картинок)
            _bucket = new GridFSBucket(_database, new GridFSBucketOptions
            {
                BucketName = "images",
                ChunkSizeBytes = 255 * 1024 // Стандартный размер чанка 255 Кб
            });
        }

        /// <summary>
        /// Вспомогательный метод генерации уникального хэша для текста промпта
        /// </summary>
        private string ComputeMd5Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant());
            byte[] hashBytes = MD5.HashData(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
        public async Task<string?> FindFileIdByPromptHashAsync(string prompt, CancellationToken cancellationToken = default)
        {
            string hash = ComputeMd5Hash(prompt);
            _logger.LogInformation("Поиск существующего изображения по хэшу промпта: {Hash}", hash);

            // Ищем файл в метаданных GridFS, где ключ "metadata.promptHash" равен нашему MD5
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.promptHash", hash);
            using var cursor = await _bucket.FindAsync(filter, cancellationToken: cancellationToken);
            var fileInfo = await cursor.FirstOrDefaultAsync(cancellationToken);

            if (fileInfo != null)
            {
                _logger.LogInformation("✅ Дубликат найден в MongoDB! ID файла: {FileId}", fileInfo.Id.ToString());
                return fileInfo.Id.ToString();
            }

            _logger.LogInformation("Дубликатов не обнаружено. Видеокарте потребуется сгенерировать новую обложку.");
            return null;
        }

        public async Task<string> SaveImageAsync(string prompt, byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            string hash = ComputeMd5Hash(prompt);
            string fileName = hash + ".webp";

            // Зашиваем хэш исходного англоязычного промпта в метаданные файла GridFS
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    { "promptHash", hash },
                    { "originalPrompt", prompt.Trim() },
                    { "uploadedAt", DateTime.UtcNow }
                }
            };

            _logger.LogInformation("Сохранение нового .webp изображения в MongoDB GridFS. Имя: {FileName}", fileName);

            var fileId = await _bucket.UploadFromBytesAsync(fileName, imageBytes, options, cancellationToken);
            return fileId.ToString();
        }

        public async Task<byte[]> GetImageBytesAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(fileId, out var objectId))
            {
                throw new ArgumentException("Некорректный формат строки MongoDB ObjectId", nameof(fileId));
            }

            _logger.LogInformation("Чтение бинарных данных изображения из GridFS. ID: {FileId}", fileId);
            return await _bucket.DownloadAsBytesAsync(objectId, cancellationToken: cancellationToken);
        }
    }
}

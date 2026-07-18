using System;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Domain.Entities
{
    public class QueueTask
    {
        public long QueueTaskId { get; set; }
        public long ArticleId { get; set; }
        public PlatformType Platform { get; set; }
        public PublicationStatus Status { get; set; } = PublicationStatus.Pending;
        public int RetryCount { get; set; } = 0;
        public string? LastError { get; set; }
        public string? LiveUrl { get; set; }
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}

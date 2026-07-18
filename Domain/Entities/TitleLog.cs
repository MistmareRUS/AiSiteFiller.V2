using System;
using AiSiteFiller.V2.Domain.Enums;

namespace AiSiteFiller.V2.Domain.Entities
{
    public class TitleLog
    {
        public long TitleLogId { get; set; }
        public PlatformType Platform { get; set; }
        public string TitleText { get; set; } = string.Empty;
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}

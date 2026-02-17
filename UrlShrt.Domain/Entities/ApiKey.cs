using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Entities
{
    public class ApiKey : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public long TotalRequests { get; set; } = 0;
        public long DailyRequests { get; set; } = 0;
        public DateTime? LastUsedAt { get; set; }
        public string? AllowedIps { get; set; } // JSON
        public int RateLimit { get; set; } = 1000; // per day

        // Navigation
        public ApplicationUser? User { get; set; }
    }

}

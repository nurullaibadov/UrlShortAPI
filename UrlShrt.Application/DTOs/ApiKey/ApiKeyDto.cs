using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Application.DTOs.ApiKey
{
    public class ApiKeyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // Masked: sk-xxxxx...xxxxx
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public long TotalRequests { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public int RateLimit { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record CreateApiKeyDto(
        string Name,
        DateTime? ExpiresAt = null,
        int RateLimit = 1000
    );
}

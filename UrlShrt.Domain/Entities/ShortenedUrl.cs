using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Entities
{
    public class ShortenedUrl : BaseEntity
    {
        public string OriginalUrl { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? CustomAlias { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Password { get; set; }
        public bool IsPasswordProtected { get; set; } = false;
        public int ClickLimit { get; set; } = 0; // 0 = unlimited
        public long TotalClicks { get; set; } = 0;
        public long UniqueClicks { get; set; } = 0;
        public string? UserId { get; set; }
        public string? Tags { get; set; } // JSON array as string
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }

        // Navigation
        public ApplicationUser? User { get; set; }
        public ICollection<UrlClick> Clicks { get; set; } = new List<UrlClick>();
        public ICollection<UrlAnalytic> Analytics { get; set; } = new List<UrlAnalytic>();
    }
}

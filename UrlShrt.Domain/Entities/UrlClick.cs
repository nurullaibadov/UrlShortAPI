using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Entities
{
    public class UrlClick : BaseEntity
    {
        public Guid ShortenedUrlId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Referer { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? DeviceType { get; set; }   // Mobile, Desktop, Tablet
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
        public bool IsUnique { get; set; }
        public string? Language { get; set; }

        // Navigation
        public ShortenedUrl? ShortenedUrl { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Entities
{
    public class UrlAnalytic : BaseEntity
    {
        public Guid ShortenedUrlId { get; set; }
        public DateTime Date { get; set; }
        public int Clicks { get; set; }
        public int UniqueClicks { get; set; }
        public string? Country { get; set; }
        public string? DeviceType { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Referer { get; set; }

        // Navigation
        public ShortenedUrl? ShortenedUrl { get; set; }
    }
}

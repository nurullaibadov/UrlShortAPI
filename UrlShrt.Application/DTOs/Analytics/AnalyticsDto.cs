using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.DTOs.Url;

namespace UrlShrt.Application.DTOs.Analytics
{
    public class UrlAnalyticsDto
    {
        public Guid UrlId { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public long TotalClicks { get; set; }
        public long UniqueClicks { get; set; }
        public DateTime? LastClickAt { get; set; }
        public List<ClickByDateDto> ClicksByDate { get; set; } = new();
        public List<ClickByCountryDto> ClicksByCountry { get; set; } = new();
        public List<ClickByDeviceDto> ClicksByDevice { get; set; } = new();
        public List<ClickByBrowserDto> ClicksByBrowser { get; set; } = new();
        public List<ClickByReferrerDto> ClicksByReferrer { get; set; } = new();
        public List<ClickByOsDto> ClicksByOs { get; set; } = new();
    }

    public record ClickByDateDto(DateTime Date, int Clicks, int UniqueClicks);
    public record ClickByCountryDto(string Country, string CountryCode, int Clicks, double Percentage);
    public record ClickByDeviceDto(string DeviceType, int Clicks, double Percentage);
    public record ClickByBrowserDto(string Browser, int Clicks, double Percentage);
    public record ClickByReferrerDto(string Referrer, int Clicks, double Percentage);
    public record ClickByOsDto(string Os, int Clicks, double Percentage);

    public class DashboardStatsDto
    {
        public long TotalUrls { get; set; }
        public long TotalClicks { get; set; }
        public long TotalUniqueClicks { get; set; }
        public long ActiveUrls { get; set; }
        public long ExpiredUrls { get; set; }
        public List<UrlResponseDto> TopUrls { get; set; } = new();
        public List<ClickByDateDto> ClicksByDate { get; set; } = new();
    }

    public class AdminDashboardStatsDto : DashboardStatsDto
    {
        public long TotalUsers { get; set; }
        public long ActiveUsers { get; set; }
        public long NewUsersThisMonth { get; set; }
        public long TotalApiKeys { get; set; }
    }
}

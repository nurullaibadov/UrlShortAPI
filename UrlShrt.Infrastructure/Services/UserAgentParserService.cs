using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Interfaces.Services;

namespace UrlShrt.Infrastructure.Services
{
    public class UserAgentParserService : IUserAgentParser
    {
        public UserAgentInfo Parse(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return new UserAgentInfo();

            var ua = userAgent.ToLowerInvariant();

            return new UserAgentInfo
            {
                Browser = DetectBrowser(ua),
                OperatingSystem = DetectOs(ua),
                DeviceType = DetectDevice(ua)
            };
        }

        private static string DetectBrowser(string ua)
        {
            if (ua.Contains("edg/") || ua.Contains("edge/")) return "Edge";
            if (ua.Contains("opr/") || ua.Contains("opera")) return "Opera";
            if (ua.Contains("chrome/")) return "Chrome";
            if (ua.Contains("firefox/")) return "Firefox";
            if (ua.Contains("safari/") && !ua.Contains("chrome")) return "Safari";
            if (ua.Contains("msie") || ua.Contains("trident")) return "Internet Explorer";
            return "Other";
        }

        private static string DetectOs(string ua)
        {
            if (ua.Contains("windows nt 11")) return "Windows 11";
            if (ua.Contains("windows nt 10")) return "Windows 10";
            if (ua.Contains("windows")) return "Windows";
            if (ua.Contains("mac os x")) return "macOS";
            if (ua.Contains("android")) return "Android";
            if (ua.Contains("iphone") || ua.Contains("ipad")) return "iOS";
            if (ua.Contains("linux")) return "Linux";
            return "Other";
        }

        private static string DetectDevice(string ua)
        {
            if (ua.Contains("bot") || ua.Contains("crawl") || ua.Contains("spider")) return "Bot";
            if (ua.Contains("tablet") || ua.Contains("ipad")) return "Tablet";
            if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone")) return "Mobile";
            return "Desktop";
        }
    }
}

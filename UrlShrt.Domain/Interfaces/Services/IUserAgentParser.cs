using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Interfaces.Services
{
    public interface IUserAgentParser
    {
        UserAgentInfo Parse(string userAgent);
    }

    public class UserAgentInfo
    {
        public string Browser { get; set; } = "Unknown";
        public string BrowserVersion { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = "Unknown";
        public string DeviceType { get; set; } = "Unknown";
    }
}

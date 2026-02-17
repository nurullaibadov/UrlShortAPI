using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Interfaces.Services
{
    public interface IGeoLocationService
    {
        Task<GeoLocationResult?> GetLocationAsync(string ipAddress, CancellationToken cancellationToken = default);
    }

    public class GeoLocationResult
    {
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Interfaces.Repositories;
using UrlShrt.Infrastructure.Data;

namespace UrlShrt.Infrastructure.Repositories
{
    public class UrlClickRepository : GenericRepository<UrlClick>, IUrlClickRepository
    {
        public UrlClickRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<UrlClick>> GetByUrlIdAsync(Guid urlId, CancellationToken ct = default)
            => await _dbSet.Where(x => x.ShortenedUrlId == urlId).OrderByDescending(x => x.ClickedAt).ToListAsync(ct);

        public async Task<bool> IsUniqueClickAsync(Guid urlId, string ipAddress, CancellationToken ct = default)
            => !await _dbSet.AnyAsync(x => x.ShortenedUrlId == urlId && x.IpAddress == ipAddress, ct);

        public async Task<IEnumerable<UrlClick>> GetRecentClicksAsync(Guid urlId, int days = 30, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            return await _dbSet.Where(x => x.ShortenedUrlId == urlId && x.ClickedAt >= since).ToListAsync(ct);
        }

        public async Task<Dictionary<string, int>> GetClicksByCountryAsync(Guid urlId, CancellationToken ct = default)
            => await _dbSet
                .Where(x => x.ShortenedUrlId == urlId && x.Country != null)
                .GroupBy(x => x.Country!)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Country, x => x.Count, ct);

        public async Task<Dictionary<string, int>> GetClicksByDeviceAsync(Guid urlId, CancellationToken ct = default)
            => await _dbSet
                .Where(x => x.ShortenedUrlId == urlId && x.DeviceType != null)
                .GroupBy(x => x.DeviceType!)
                .Select(g => new { Device = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Device, x => x.Count, ct);

        public async Task<Dictionary<string, int>> GetClicksByBrowserAsync(Guid urlId, CancellationToken ct = default)
            => await _dbSet
                .Where(x => x.ShortenedUrlId == urlId && x.Browser != null)
                .GroupBy(x => x.Browser!)
                .Select(g => new { Browser = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Browser, x => x.Count, ct);

        public async Task<Dictionary<DateTime, int>> GetClicksByDateAsync(Guid urlId, int days = 30, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-days).Date;
            return await _dbSet
                .Where(x => x.ShortenedUrlId == urlId && x.ClickedAt >= since)
                .GroupBy(x => x.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        }
    }
}

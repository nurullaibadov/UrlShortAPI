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
    public class UrlRepository : GenericRepository<ShortenedUrl>, IUrlRepository
    {
        public UrlRepository(AppDbContext context) : base(context) { }

        public async Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.ShortCode == shortCode, ct);

        public async Task<ShortenedUrl?> GetByCustomAliasAsync(string alias, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.CustomAlias == alias, ct);

        public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct = default)
            => await _dbSet.AnyAsync(x => x.ShortCode == shortCode, ct);

        public async Task<bool> CustomAliasExistsAsync(string alias, CancellationToken ct = default)
            => await _dbSet.AnyAsync(x => x.CustomAlias == alias, ct);

        public async Task<IEnumerable<ShortenedUrl>> GetByUserIdAsync(string userId, CancellationToken ct = default)
            => await _dbSet.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        public async Task<(IEnumerable<ShortenedUrl> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId, int page, int pageSize, string? search = null, CancellationToken ct = default)
        {
            var query = _dbSet.Where(x => x.UserId == userId);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    x.OriginalUrl.Contains(search) ||
                    x.ShortCode.Contains(search) ||
                    (x.Title != null && x.Title.Contains(search)) ||
                    (x.CustomAlias != null && x.CustomAlias.Contains(search)));

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return (items, totalCount);
        }

        public async Task IncrementClickCountAsync(Guid urlId, CancellationToken ct = default)
        {
            await _dbSet
                .Where(x => x.Id == urlId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TotalClicks, x => x.TotalClicks + 1), ct);
        }

        public async Task<ShortenedUrl?> GetWithClicksAsync(Guid id, CancellationToken ct = default)
            => await _dbSet.Include(x => x.Clicks).FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<IEnumerable<ShortenedUrl>> GetExpiredUrlsAsync(CancellationToken ct = default)
            => await _dbSet.Where(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value < DateTime.UtcNow && x.IsActive).ToListAsync(ct);

        public async Task<IEnumerable<ShortenedUrl>> GetTopUrlsByClicksAsync(int count = 10, CancellationToken ct = default)
            => await _dbSet.OrderByDescending(x => x.TotalClicks).Take(count).ToListAsync(ct);

        public async Task<long> GetTotalClicksForUserAsync(string userId, CancellationToken ct = default)
            => await _dbSet.Where(x => x.UserId == userId).SumAsync(x => x.TotalClicks, ct);
    }
}

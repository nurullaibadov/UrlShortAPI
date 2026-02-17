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
    public class ApiKeyRepository : GenericRepository<ApiKey>, IApiKeyRepository
    {
        public ApiKeyRepository(AppDbContext context) : base(context) { }

        public async Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.Key == key && x.IsActive, ct);

        public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId, CancellationToken ct = default)
            => await _dbSet.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        public async Task IncrementRequestCountAsync(string key, CancellationToken ct = default)
        {
            await _dbSet
                .Where(x => x.Key == key)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TotalRequests, x => x.TotalRequests + 1)
                    .SetProperty(x => x.LastUsedAt, DateTime.UtcNow), ct);
        }
    }
}

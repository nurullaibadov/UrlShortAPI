using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UrlShrt.Domain.Interfaces.Services;

namespace UrlShrt.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached is null) return default;
            return JsonSerializer.Deserialize<T>(cached);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
            };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => await _cache.RemoveAsync(key, cancellationToken);

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            => await _cache.GetStringAsync(key, cancellationToken) is not null;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            // Pattern-based removal requires Redis. For in-memory cache, this is a no-op.
            // Implement with IConnectionMultiplexer for Redis in production.
            return Task.CompletedTask;
        }
    }
}

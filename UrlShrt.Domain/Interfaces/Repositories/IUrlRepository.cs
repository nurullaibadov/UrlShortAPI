using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Domain.Interfaces.Repositories
{
    public interface IUrlRepository : IRepository<ShortenedUrl>
    {
        Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
        Task<ShortenedUrl?> GetByCustomAliasAsync(string alias, CancellationToken cancellationToken = default);
        Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
        Task<bool> CustomAliasExistsAsync(string alias, CancellationToken cancellationToken = default);
        Task<IEnumerable<ShortenedUrl>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ShortenedUrl> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId, int page, int pageSize, string? search = null,
            CancellationToken cancellationToken = default);
        Task IncrementClickCountAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<ShortenedUrl?> GetWithClicksAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ShortenedUrl>> GetExpiredUrlsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ShortenedUrl>> GetTopUrlsByClicksAsync(int count = 10, CancellationToken cancellationToken = default);
        Task<long> GetTotalClicksForUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}

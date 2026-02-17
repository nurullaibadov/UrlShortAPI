using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Domain.Interfaces.Repositories
{
    public interface IApiKeyRepository : IRepository<ApiKey>
    {
        Task<ApiKey?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task IncrementRequestCountAsync(string key, CancellationToken cancellationToken = default);
    }
}

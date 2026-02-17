using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Domain.Interfaces.Repositories
{
    public interface IUrlClickRepository : IRepository<UrlClick>
    {
        Task<IEnumerable<UrlClick>> GetByUrlIdAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<bool> IsUniqueClickAsync(Guid urlId, string ipAddress, CancellationToken cancellationToken = default);
        Task<IEnumerable<UrlClick>> GetRecentClicksAsync(Guid urlId, int days = 30, CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetClicksByCountryAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetClicksByDeviceAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetClicksByBrowserAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<Dictionary<DateTime, int>> GetClicksByDateAsync(Guid urlId, int days = 30, CancellationToken cancellationToken = default);
    }
}

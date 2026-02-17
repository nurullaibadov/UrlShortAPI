using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Analytics;

namespace UrlShrt.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<ApiResponse<UrlAnalyticsDto>> GetUrlAnalyticsAsync(Guid urlId, string? userId, int days = 30, CancellationToken cancellationToken = default);
        Task<ApiResponse<DashboardStatsDto>> GetUserDashboardAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<AdminDashboardStatsDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
        Task RecordClickAsync(Guid urlId, string ipAddress, string userAgent, string? referer, CancellationToken cancellationToken = default);
    }
}

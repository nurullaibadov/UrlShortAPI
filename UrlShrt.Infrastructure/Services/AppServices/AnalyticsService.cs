using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Analytics;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Application.Interfaces;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Interfaces.Repositories;
using UrlShrt.Domain.Interfaces.Services;
using UrlShrt.Infrastructure.Data;

namespace UrlShrt.Infrastructure.Services.AppServices
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUrlRepository _urlRepo;
        private readonly IUrlClickRepository _clickRepo;
        private readonly IUserAgentParser _uaParser;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AnalyticsService(
            IUrlRepository urlRepo,
            IUrlClickRepository clickRepo,
            IUserAgentParser uaParser,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IMapper mapper)
        {
            _urlRepo = urlRepo;
            _clickRepo = clickRepo;
            _uaParser = uaParser;
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<UrlAnalyticsDto>> GetUrlAnalyticsAsync(Guid urlId, string? userId, int days = 30, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(urlId, ct);
            if (url is null) return ApiResponse<UrlAnalyticsDto>.NotFound("URL not found.");

            // Authorization: owner or admin
            if (userId is not null && url.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null) return ApiResponse<UrlAnalyticsDto>.Forbidden("Access denied.");
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
                    return ApiResponse<UrlAnalyticsDto>.Forbidden("Access denied.");
            }

            var since = DateTime.UtcNow.AddDays(-days).Date;
            var clicks = await _context.UrlClicks
                .Where(x => x.ShortenedUrlId == urlId && x.ClickedAt >= since)
                .ToListAsync(ct);

            var lastClick = await _context.UrlClicks
                .Where(x => x.ShortenedUrlId == urlId)
                .OrderByDescending(x => x.ClickedAt)
                .Select(x => (DateTime?)x.ClickedAt)
                .FirstOrDefaultAsync(ct);

            // Build date series (fill gaps with 0)
            var dateRange = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .ToList();

            var clicksByDate = clicks
                .GroupBy(x => x.ClickedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var uniqueClicksByDate = clicks
                .Where(x => x.IsUnique)
                .GroupBy(x => x.ClickedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var analytics = new UrlAnalyticsDto
            {
                UrlId = url.Id,
                ShortCode = url.ShortCode,
                ShortUrl = url.ShortUrl,
                OriginalUrl = url.OriginalUrl,
                TotalClicks = url.TotalClicks,
                UniqueClicks = url.UniqueClicks,
                LastClickAt = lastClick,

                ClicksByDate = dateRange.Select(date => new ClickByDateDto(
                    date,
                    clicksByDate.GetValueOrDefault(date, 0),
                    uniqueClicksByDate.GetValueOrDefault(date, 0)
                )).ToList(),

                ClicksByCountry = BuildPercentageList(
                    clicks.Where(x => x.Country != null)
                          .GroupBy(x => x.Country!)
                          .OrderByDescending(g => g.Count()),
                    g => new ClickByCountryDto(g.Key, g.Key, g.Count(), 0),
                    clicks.Count
                ).Cast<ClickByCountryDto>().ToList(),

                ClicksByDevice = clicks
                    .Where(x => x.DeviceType != null)
                    .GroupBy(x => x.DeviceType!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new ClickByDeviceDto(
                        g.Key, g.Count(),
                        clicks.Count > 0 ? Math.Round((double)g.Count() / clicks.Count * 100, 2) : 0))
                    .ToList(),

                ClicksByBrowser = clicks
                    .Where(x => x.Browser != null)
                    .GroupBy(x => x.Browser!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new ClickByBrowserDto(
                        g.Key, g.Count(),
                        clicks.Count > 0 ? Math.Round((double)g.Count() / clicks.Count * 100, 2) : 0))
                    .ToList(),

                ClicksByReferrer = clicks
                    .Where(x => x.Referer != null)
                    .GroupBy(x => x.Referer!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new ClickByReferrerDto(
                        g.Key, g.Count(),
                        clicks.Count > 0 ? Math.Round((double)g.Count() / clicks.Count * 100, 2) : 0))
                    .ToList(),

                ClicksByOs = clicks
                    .Where(x => x.OperatingSystem != null)
                    .GroupBy(x => x.OperatingSystem!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new ClickByOsDto(
                        g.Key, g.Count(),
                        clicks.Count > 0 ? Math.Round((double)g.Count() / clicks.Count * 100, 2) : 0))
                    .ToList()
            };

            return ApiResponse<UrlAnalyticsDto>.Ok(analytics);
        }

        public async Task<ApiResponse<DashboardStatsDto>> GetUserDashboardAsync(string userId, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-30).Date;

            var totalUrls = await _context.ShortenedUrls.CountAsync(x => x.UserId == userId, ct);
            var activeUrls = await _context.ShortenedUrls.CountAsync(x => x.UserId == userId && x.IsActive, ct);
            var expiredUrls = await _context.ShortenedUrls.CountAsync(x => x.UserId == userId && x.ExpiresAt < DateTime.UtcNow, ct);
            var totalClicks = await _context.ShortenedUrls.Where(x => x.UserId == userId).SumAsync(x => x.TotalClicks, ct);
            var totalUniqueClicks = await _context.ShortenedUrls.Where(x => x.UserId == userId).SumAsync(x => x.UniqueClicks, ct);

            var topUrls = await _context.ShortenedUrls
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.TotalClicks)
                .Take(5)
                .ToListAsync(ct);

            var userUrlIds = await _context.ShortenedUrls
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var clicksByDate = await _context.UrlClicks
                .Where(x => userUrlIds.Contains(x.ShortenedUrlId) && x.ClickedAt >= since)
                .GroupBy(x => x.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var stats = new DashboardStatsDto
            {
                TotalUrls = totalUrls,
                TotalClicks = totalClicks,
                TotalUniqueClicks = totalUniqueClicks,
                ActiveUrls = activeUrls,
                ExpiredUrls = expiredUrls,
                TopUrls = _mapper.Map<List<UrlResponseDto>>(topUrls),
                ClicksByDate = Enumerable.Range(0, 30)
                    .Select(i => since.AddDays(i))
                    .Select(d => new ClickByDateDto(
                        d,
                        clicksByDate.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
                        0))
                    .ToList()
            };

            return ApiResponse<DashboardStatsDto>.Ok(stats);
        }

        public async Task<ApiResponse<AdminDashboardStatsDto>> GetAdminDashboardAsync(CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-30).Date;
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var totalUrls = await _context.ShortenedUrls.CountAsync(ct);
            var activeUrls = await _context.ShortenedUrls.CountAsync(x => x.IsActive, ct);
            var expiredUrls = await _context.ShortenedUrls.CountAsync(x => x.ExpiresAt < DateTime.UtcNow, ct);
            var totalClicks = await _context.ShortenedUrls.SumAsync(x => x.TotalClicks, ct);
            var totalUniqueClicks = await _context.ShortenedUrls.SumAsync(x => x.UniqueClicks, ct);
            var totalUsers = await _userManager.Users.CountAsync(ct);
            var activeUsers = await _userManager.Users.CountAsync(x => x.IsActive, ct);
            var newUsersThisMonth = await _userManager.Users.CountAsync(x => x.CreatedAt >= monthStart, ct);
            var totalApiKeys = await _context.ApiKeys.CountAsync(ct);

            var topUrls = await _context.ShortenedUrls
                .OrderByDescending(x => x.TotalClicks)
                .Take(10)
                .ToListAsync(ct);

            var clicksByDate = await _context.UrlClicks
                .Where(x => x.ClickedAt >= since)
                .GroupBy(x => x.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var stats = new AdminDashboardStatsDto
            {
                TotalUrls = totalUrls,
                TotalClicks = totalClicks,
                TotalUniqueClicks = totalUniqueClicks,
                ActiveUrls = activeUrls,
                ExpiredUrls = expiredUrls,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                NewUsersThisMonth = newUsersThisMonth,
                TotalApiKeys = totalApiKeys,
                TopUrls = _mapper.Map<List<UrlResponseDto>>(topUrls),
                ClicksByDate = Enumerable.Range(0, 30)
                    .Select(i => since.AddDays(i))
                    .Select(d => new ClickByDateDto(
                        d,
                        clicksByDate.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
                        0))
                    .ToList()
            };

            return ApiResponse<AdminDashboardStatsDto>.Ok(stats);
        }

        public async Task RecordClickAsync(Guid urlId, string ipAddress, string userAgent, string? referer, CancellationToken ct = default)
        {
            var isUnique = await _clickRepo.IsUniqueClickAsync(urlId, ipAddress, ct);
            var uaInfo = _uaParser.Parse(userAgent);

            var click = new UrlClick
            {
                ShortenedUrlId = urlId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Referer = referer,
                DeviceType = uaInfo.DeviceType,
                Browser = uaInfo.Browser,
                OperatingSystem = uaInfo.OperatingSystem,
                ClickedAt = DateTime.UtcNow,
                IsUnique = isUnique,
                Country = "Unknown" // GeoIP integration point
            };

            await _clickRepo.AddAsync(click, ct);

            if (isUnique)
            {
                await _context.ShortenedUrls
                    .Where(x => x.Id == urlId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.UniqueClicks, x => x.UniqueClicks + 1), ct);
            }
        }

        private static IEnumerable<object> BuildPercentageList<T>(
            IEnumerable<IGrouping<string, T>> groups,
            Func<IGrouping<string, T>, object> selector,
            int total) where T : class
            => groups.Select(selector);
    }
}

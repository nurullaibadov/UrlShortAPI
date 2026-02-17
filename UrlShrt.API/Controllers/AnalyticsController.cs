using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [Authorize]
    [SwaggerTag("Analytics — URL click statistics and insights")]
    public class AnalyticsController : BaseController
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>Get analytics for a specific URL</summary>
        [HttpGet("url/{urlId:guid}")]
        [SwaggerOperation(Summary = "Get URL Analytics")]
        public async Task<IActionResult> GetUrlAnalytics(
            Guid urlId,
            [FromQuery] int days = 30,
            CancellationToken ct = default)
        {
            var result = await _analyticsService.GetUrlAnalyticsAsync(urlId, CurrentUserId, days, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get user dashboard statistics</summary>
        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get Dashboard Stats")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var result = await _analyticsService.GetUserDashboardAsync(CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get admin dashboard statistics</summary>
        [HttpGet("admin/dashboard")]
        [Authorize(Policy = "AdminOnly")]
        [SwaggerOperation(Summary = "Get Admin Dashboard")]
        public async Task<IActionResult> GetAdminDashboard(CancellationToken ct)
        {
            var result = await _analyticsService.GetAdminDashboardAsync(ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

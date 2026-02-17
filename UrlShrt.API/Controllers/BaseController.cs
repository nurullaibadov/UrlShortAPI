using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UrlShrt.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController : ControllerBase
    {
        protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        protected string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);
        protected bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        protected string GetClientIpAddress()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        protected string GetUserAgent()
            => HttpContext.Request.Headers["User-Agent"].ToString();

        protected string? GetReferer()
            => HttpContext.Request.Headers["Referer"].ToString();
    }
}

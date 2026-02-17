using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [ApiController]
    [Route("")]
    [ApiExplorerSettings(GroupName = "v1")]
    [SwaggerTag("Redirect — The core redirect functionality")]
    public class RedirectController : ControllerBase
    {
        private readonly IUrlService _urlService;

        public RedirectController(IUrlService urlService)
        {
            _urlService = urlService;
        }

        private string GetClientIp()
        {
            var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrEmpty(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>Redirect to original URL</summary>
        [HttpGet("{shortCode}")]
        [SwaggerOperation(Summary = "Redirect to original URL")]
        public async Task<IActionResult> Redirect(string shortCode, CancellationToken ct)
        {
            var ip = GetClientIp();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();
            var referer = HttpContext.Request.Headers["Referer"].ToString();

            var result = await _urlService.ResolveAsync(shortCode, ip, ua, referer, ct);

            if (!result.Success)
            {
                // 401 = password protected
                if (result.StatusCode == 401)
                    return Redirect($"/password-required/{shortCode}");
                // 410 = expired or inactive
                if (result.StatusCode == 410)
                    return Redirect("/link-expired");
                return NotFound(result);
            }

            return Redirect(result.Data!);
        }

        /// <summary>Redirect with password</summary>
        [HttpPost("{shortCode}/unlock")]
        [SwaggerOperation(Summary = "Unlock password-protected URL")]
        public async Task<IActionResult> UnlockAndRedirect(string shortCode, [FromBody] VerifyUrlPasswordDto dto, CancellationToken ct)
        {
            var ip = GetClientIp();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();
            var referer = HttpContext.Request.Headers["Referer"].ToString();

            var result = await _urlService.ResolveWithPasswordAsync(shortCode, dto.Password, ip, ua, referer, ct);

            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }
    }
}

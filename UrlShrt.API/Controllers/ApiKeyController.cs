using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.DTOs.ApiKey;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [Authorize]
    [SwaggerTag("API Keys — Manage programmatic access keys")]
    public class ApiKeyController : BaseController
    {
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        /// <summary>Create a new API key</summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Create API Key", Description = "The full key is only shown once on creation")]
        public async Task<IActionResult> Create([FromBody] CreateApiKeyDto dto, CancellationToken ct)
        {
            var result = await _apiKeyService.CreateAsync(CurrentUserId!, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>List all API keys for current user</summary>
        [HttpGet]
        [SwaggerOperation(Summary = "List API Keys")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _apiKeyService.GetByUserIdAsync(CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Revoke an API key</summary>
        [HttpDelete("{id:guid}")]
        [SwaggerOperation(Summary = "Revoke API Key")]
        public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
        {
            var result = await _apiKeyService.RevokeAsync(id, CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

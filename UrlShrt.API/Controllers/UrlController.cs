using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [SwaggerTag("URL Management — Create, Update, Delete, List URLs")]
    public class UrlController : BaseController
    {
        private readonly IUrlService _urlService;
        private readonly IValidator<CreateUrlDto> _createValidator;
        private readonly IValidator<UpdateUrlDto> _updateValidator;

        public UrlController(
            IUrlService urlService,
            IValidator<CreateUrlDto> createValidator,
            IValidator<UpdateUrlDto> updateValidator)
        {
            _urlService = urlService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <summary>Create a shortened URL (anonymous or authenticated)</summary>
        [HttpPost]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Create Short URL")]
        public async Task<IActionResult> Create([FromBody] CreateUrlDto dto, CancellationToken ct)
        {
            var validation = await _createValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _urlService.CreateAsync(CurrentUserId, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Bulk create URLs (authenticated)</summary>
        [HttpPost("bulk")]
        [Authorize]
        [SwaggerOperation(Summary = "Bulk Create URLs")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkCreateUrlDto dto, CancellationToken ct)
        {
            var result = await _urlService.BulkCreateAsync(CurrentUserId!, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get all URLs for current user</summary>
        [HttpGet("my")]
        [Authorize]
        [SwaggerOperation(Summary = "Get My URLs")]
        public async Task<IActionResult> GetMyUrls([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _urlService.GetUserUrlsAsync(CurrentUserId!, request, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get URL by ID</summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [SwaggerOperation(Summary = "Get URL by ID")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _urlService.GetByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get URL info by short code</summary>
        [HttpGet("code/{shortCode}")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get URL by Short Code")]
        public async Task<IActionResult> GetByShortCode(string shortCode, CancellationToken ct)
        {
            var result = await _urlService.GetByShortCodeAsync(shortCode, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Update URL metadata</summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        [SwaggerOperation(Summary = "Update URL")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUrlDto dto, CancellationToken ct)
        {
            var validation = await _updateValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _urlService.UpdateAsync(id, CurrentUserId!, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a URL</summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete URL")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _urlService.DeleteAsync(id, CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Toggle active/inactive status</summary>
        [HttpPatch("{id:guid}/toggle")]
        [Authorize]
        [SwaggerOperation(Summary = "Toggle URL Active Status")]
        public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
        {
            var result = await _urlService.ToggleActiveAsync(id, CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get QR code for URL</summary>
        [HttpGet("{id:guid}/qr")]
        [Authorize]
        [SwaggerOperation(Summary = "Get QR Code (Base64 PNG)")]
        public async Task<IActionResult> GetQrCode(Guid id, CancellationToken ct)
        {
            var result = await _urlService.GenerateQrCodeAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Check password for a protected URL</summary>
        [HttpPost("check-password/{shortCode}")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Check URL Password")]
        public async Task<IActionResult> CheckPassword(string shortCode, [FromBody] VerifyUrlPasswordDto dto, CancellationToken ct)
        {
            var result = await _urlService.CheckPasswordAsync(shortCode, dto.Password, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

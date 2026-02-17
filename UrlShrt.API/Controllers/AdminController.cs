using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Admin;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [SwaggerTag("Admin Panel — User and URL management for administrators")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ─── USER MANAGEMENT ──────────────────────────────────────────────────────

        /// <summary>Get paginated user list</summary>
        [HttpGet("users")]
        [SwaggerOperation(Summary = "List All Users")]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _adminService.GetUsersAsync(request, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get user by ID</summary>
        [HttpGet("users/{id}")]
        [SwaggerOperation(Summary = "Get User by ID")]
        public async Task<IActionResult> GetUser(string id, CancellationToken ct)
        {
            var result = await _adminService.GetUserByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new user (admin only)</summary>
        [HttpPost("users")]
        [SwaggerOperation(Summary = "Create User")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminDto dto, CancellationToken ct)
        {
            var result = await _adminService.CreateUserAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Update user role</summary>
        [HttpPut("users/role")]
        [Authorize(Policy = "SuperAdminOnly")]
        [SwaggerOperation(Summary = "Update User Role")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateUserRoleDto dto, CancellationToken ct)
        {
            var result = await _adminService.UpdateUserRoleAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Activate or deactivate a user</summary>
        [HttpPut("users/status")]
        [SwaggerOperation(Summary = "Toggle User Status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateUserStatusDto dto, CancellationToken ct)
        {
            var result = await _adminService.UpdateUserStatusAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Update user subscription plan</summary>
        [HttpPut("users/plan")]
        [SwaggerOperation(Summary = "Update User Plan")]
        public async Task<IActionResult> UpdatePlan([FromBody] UpdateUserPlanDto dto, CancellationToken ct)
        {
            var result = await _adminService.UpdateUserPlanAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Soft delete user</summary>
        [HttpDelete("users/{id}")]
        [Authorize(Policy = "SuperAdminOnly")]
        [SwaggerOperation(Summary = "Delete User")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken ct)
        {
            var result = await _adminService.DeleteUserAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Lock user account</summary>
        [HttpPost("users/{id}/lock")]
        [SwaggerOperation(Summary = "Lock User Account")]
        public async Task<IActionResult> LockUser(string id, CancellationToken ct)
        {
            var result = await _adminService.LockUserAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Unlock user account</summary>
        [HttpPost("users/{id}/unlock")]
        [SwaggerOperation(Summary = "Unlock User Account")]
        public async Task<IActionResult> UnlockUser(string id, CancellationToken ct)
        {
            var result = await _adminService.UnlockUserAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        // ─── URL MANAGEMENT ────────────────────────────────────────────────────────

        /// <summary>Get all URLs across all users</summary>
        [HttpGet("urls")]
        [SwaggerOperation(Summary = "List All URLs")]
        public async Task<IActionResult> GetAllUrls([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _adminService.GetAllUrlsAsync(request, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a URL</summary>
        [HttpDelete("urls/{urlId:guid}")]
        [SwaggerOperation(Summary = "Admin Delete URL")]
        public async Task<IActionResult> DeleteUrl(Guid urlId, CancellationToken ct)
        {
            var result = await _adminService.AdminDeleteUrlAsync(urlId, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Deactivate all expired URLs</summary>
        [HttpPost("urls/clean-expired")]
        [SwaggerOperation(Summary = "Clean Expired URLs")]
        public async Task<IActionResult> CleanExpired(CancellationToken ct)
        {
            var result = await _adminService.CleanExpiredUrlsAsync(ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

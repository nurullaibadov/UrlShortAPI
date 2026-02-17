using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Admin;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Application.Interfaces;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Interfaces.Repositories;
using UrlShrt.Infrastructure.Data;

namespace UrlShrt.Infrastructure.Services.AppServices
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IUrlRepository _urlRepo;
        private readonly IMapper _mapper;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IUrlRepository urlRepo,
            IMapper mapper)
        {
            _userManager = userManager;
            _context = context;
            _urlRepo = urlRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var query = _userManager.Users.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x =>
                    x.Email!.Contains(request.Search) ||
                    x.FirstName.Contains(request.Search) ||
                    x.LastName.Contains(request.Search));

            var total = await query.CountAsync(ct);
            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var dtos = new List<AdminUserDto>();
            foreach (var user in users)
            {
                var dto = _mapper.Map<AdminUserDto>(user);
                dto.Roles = await _userManager.GetRolesAsync(user);
                dto.TotalUrls = await _context.ShortenedUrls.CountAsync(x => x.UserId == user.Id, ct);
                dto.TotalClicks = await _context.ShortenedUrls
                    .Where(x => x.UserId == user.Id)
                    .SumAsync(x => x.TotalClicks, ct);
                dtos.Add(dto);
            }

            return ApiResponse<PagedResult<AdminUserDto>>.Ok(
                PagedResult<AdminUserDto>.Create(dtos, total, request.Page, request.PageSize));
        }

        public async Task<ApiResponse<AdminUserDto>> GetUserByIdAsync(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return ApiResponse<AdminUserDto>.NotFound("User not found.");

            var dto = _mapper.Map<AdminUserDto>(user);
            dto.Roles = await _userManager.GetRolesAsync(user);
            dto.TotalUrls = await _context.ShortenedUrls.CountAsync(x => x.UserId == user.Id, ct);
            dto.TotalClicks = await _context.ShortenedUrls
                .Where(x => x.UserId == user.Id)
                .SumAsync(x => x.TotalClicks, ct);

            return ApiResponse<AdminUserDto>.Ok(dto);
        }

        public async Task<ApiResponse<AdminUserDto>> CreateUserAsync(CreateUserByAdminDto dto, CancellationToken ct = default)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                return ApiResponse<AdminUserDto>.Conflict("Email already registered.");

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true,
                IsActive = true,
                Plan = dto.Plan,
                UrlLimit = dto.UrlLimit
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return ApiResponse<AdminUserDto>.Fail(
                    string.Join(", ", result.Errors.Select(e => e.Description)), 400);

            await _userManager.AddToRoleAsync(user, dto.Role);

            var responseDto = _mapper.Map<AdminUserDto>(user);
            responseDto.Roles = new List<string> { dto.Role };
            return ApiResponse<AdminUserDto>.Created(responseDto, "User created successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateUserRoleAsync(UpdateUserRoleDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            return ApiResponse<bool>.Ok(true, $"User role updated to {dto.Role}.");
        }

        public async Task<ApiResponse<bool>> UpdateUserStatusAsync(UpdateUserStatusDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            user.IsActive = dto.IsActive;
            await _userManager.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true, dto.IsActive ? "User activated." : "User deactivated.");
        }

        public async Task<ApiResponse<bool>> UpdateUserPlanAsync(UpdateUserPlanDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            user.Plan = dto.Plan;
            user.UrlLimit = dto.UrlLimit;
            await _userManager.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true, $"Plan updated to {dto.Plan}.");
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true, "User deleted.");
        }

        public async Task<ApiResponse<bool>> LockUserAsync(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return ApiResponse<bool>.Ok(true, "User locked.");
        }

        public async Task<ApiResponse<bool>> UnlockUserAsync(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return ApiResponse<bool>.NotFound("User not found.");

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            return ApiResponse<bool>.Ok(true, "User unlocked.");
        }

        public async Task<ApiResponse<PagedResult<UrlResponseDto>>> GetAllUrlsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var query = _context.ShortenedUrls.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x =>
                    x.OriginalUrl.Contains(request.Search) ||
                    x.ShortCode.Contains(request.Search));

            var total = await query.CountAsync(ct);
            var urls = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return ApiResponse<PagedResult<UrlResponseDto>>.Ok(
                PagedResult<UrlResponseDto>.Create(
                    _mapper.Map<IEnumerable<UrlResponseDto>>(urls), total, request.Page, request.PageSize));
        }

        public async Task<ApiResponse<bool>> AdminDeleteUrlAsync(Guid urlId, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(urlId, ct);
            if (url is null) return ApiResponse<bool>.NotFound("URL not found.");

            await _urlRepo.SoftDeleteAsync(url, ct);
            return ApiResponse<bool>.Ok(true, "URL deleted.");
        }

        public async Task<ApiResponse<bool>> CleanExpiredUrlsAsync(CancellationToken ct = default)
        {
            var expired = await _urlRepo.GetExpiredUrlsAsync(ct);
            var count = 0;
            foreach (var url in expired)
            {
                url.IsActive = false;
                await _urlRepo.UpdateAsync(url, ct);
                count++;
            }

            return ApiResponse<bool>.Ok(true, $"{count} expired URLs deactivated.");
        }
    }
}

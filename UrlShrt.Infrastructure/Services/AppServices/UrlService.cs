using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Url;
using UrlShrt.Application.Interfaces;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Exceptions;
using UrlShrt.Domain.Interfaces.Repositories;
using UrlShrt.Domain.Interfaces.Services;

namespace UrlShrt.Infrastructure.Services.AppServices
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _urlRepo;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cache;
        private readonly IAnalyticsService _analyticsService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string CachePrefix = "url:";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public UrlService(
            IUrlRepository urlRepo,
            ITokenService tokenService,
            ICacheService cache,
            IAnalyticsService analyticsService,
            IMapper mapper,
            IConfiguration config,
            UserManager<ApplicationUser> userManager)
        {
            _urlRepo = urlRepo;
            _tokenService = tokenService;
            _cache = cache;
            _analyticsService = analyticsService;
            _mapper = mapper;
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Create a new shortened URL
        /// </summary>
        public async Task<ApiResponse<UrlResponseDto>> CreateAsync(string? userId, CreateUrlDto dto, CancellationToken ct = default)
        {
            // Check user URL limit if authenticated
            if (userId is not null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is not null)
                {
                    var userUrlCount = await _urlRepo.CountAsync(x => x.UserId == userId, ct);
                    if (userUrlCount >= user.UrlLimit)
                    {
                        throw new UrlLimitExceededException();
                    }
                }
            }

            // Validate custom alias uniqueness
            if (!string.IsNullOrWhiteSpace(dto.CustomAlias))
            {
                if (await _urlRepo.CustomAliasExistsAsync(dto.CustomAlias, ct))
                {
                    return ApiResponse<UrlResponseDto>.Conflict($"Custom alias '{dto.CustomAlias}' is already in use.");
                }
            }

            // Generate unique short code
            var shortCode = !string.IsNullOrWhiteSpace(dto.CustomAlias)
                ? dto.CustomAlias
                : await GenerateUniqueShortCodeAsync(ct);

            var baseUrl = _config["AppSettings:ShortUrlBase"] ?? throw new InvalidOperationException("ShortUrlBase not configured");

            var url = _mapper.Map<ShortenedUrl>(dto);
            url.ShortCode = shortCode;
            url.ShortUrl = $"{baseUrl}/{shortCode}";
            url.UserId = userId;
            url.CreatedAt = DateTime.UtcNow;
            url.IsActive = true;

            // Hash password if provided
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                url.Password = _tokenService.HashPassword(dto.Password);
                url.IsPasswordProtected = true;
            }

            // Serialize tags if provided
            if (dto.Tags is not null && dto.Tags.Any())
            {
                url.Tags = JsonSerializer.Serialize(dto.Tags);
            }

            await _urlRepo.AddAsync(url, ct);

            var response = _mapper.Map<UrlResponseDto>(url);

            // Generate QR code if enabled
            if (bool.Parse(_config["AppSettings:EnableQrCode"] ?? "true"))
            {
                response.QrCodeUrl = GenerateQrCodeBase64(url.ShortUrl);
            }

            return ApiResponse<UrlResponseDto>.Created(response, "URL shortened successfully.");
        }

        /// <summary>
        /// Get URL by ID
        /// </summary>
        public async Task<ApiResponse<UrlResponseDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(id, ct);
            if (url is null)
            {
                return ApiResponse<UrlResponseDto>.NotFound("URL not found.");
            }

            var response = _mapper.Map<UrlResponseDto>(url);
            return ApiResponse<UrlResponseDto>.Ok(response);
        }

        /// <summary>
        /// Get URL by short code
        /// </summary>
        public async Task<ApiResponse<UrlResponseDto>> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
        {
            var url = await GetUrlByCodeOrAliasAsync(shortCode, ct);
            if (url is null)
            {
                return ApiResponse<UrlResponseDto>.NotFound("URL not found.");
            }

            var response = _mapper.Map<UrlResponseDto>(url);
            return ApiResponse<UrlResponseDto>.Ok(response);
        }

        /// <summary>
        /// Resolve short code to original URL (for redirect)
        /// </summary>
        public async Task<ApiResponse<string>> ResolveAsync(
            string shortCode,
            string ipAddress,
            string userAgent,
            string? referer,
            CancellationToken ct = default)
        {
            // Try to get from cache first
            var cacheKey = $"{CachePrefix}{shortCode}";
            var cached = await _cache.GetAsync<CachedUrlData>(cacheKey, ct);

            ShortenedUrl? url = null;

            if (cached is not null)
            {
                // Validate cached data is still valid
                url = await _urlRepo.GetByShortCodeAsync(shortCode, ct)
                      ?? await _urlRepo.GetByCustomAliasAsync(shortCode, ct);
            }
            else
            {
                url = await GetUrlByCodeOrAliasAsync(shortCode, ct);
            }

            if (url is null)
            {
                return ApiResponse<string>.NotFound("URL not found.");
            }

            // Validation checks
            if (!url.IsActive)
            {
                return ApiResponse<string>.Fail("This URL has been deactivated.", 410);
            }

            if (url.ExpiresAt.HasValue && url.ExpiresAt.Value < DateTime.UtcNow)
            {
                throw new UrlExpiredException();
            }

            if (url.ClickLimit > 0 && url.TotalClicks >= url.ClickLimit)
            {
                return ApiResponse<string>.Fail("This URL has reached its click limit.", 410);
            }

            if (url.IsPasswordProtected)
            {
                throw new PasswordRequiredException();
            }

            // Record click asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _analyticsService.RecordClickAsync(url.Id, ipAddress, userAgent, referer, CancellationToken.None);
                    await _urlRepo.IncrementClickCountAsync(url.Id, CancellationToken.None);
                }
                catch
                {
                    // Log error but don't fail the redirect
                }
            }, CancellationToken.None);

            // Cache the URL data
            await _cache.SetAsync(cacheKey, new CachedUrlData(url.OriginalUrl, url.IsPasswordProtected), CacheExpiry, ct);

            // Append UTM parameters if configured
            var targetUrl = AppendUtmParams(url);

            return ApiResponse<string>.Ok(targetUrl);
        }

        /// <summary>
        /// Resolve password-protected URL
        /// </summary>
        public async Task<ApiResponse<string>> ResolveWithPasswordAsync(
            string shortCode,
            string password,
            string ipAddress,
            string userAgent,
            string? referer,
            CancellationToken ct = default)
        {
            var url = await GetUrlByCodeOrAliasAsync(shortCode, ct);
            if (url is null)
            {
                return ApiResponse<string>.NotFound("URL not found.");
            }

            // If not password protected, just resolve normally
            if (!url.IsPasswordProtected || url.Password is null)
            {
                return await ResolveAsync(shortCode, ipAddress, userAgent, referer, ct);
            }

            // Verify password
            if (!_tokenService.VerifyPassword(password, url.Password))
            {
                return ApiResponse<string>.Fail("Incorrect password.", 401);
            }

            // Validation checks
            if (!url.IsActive)
            {
                return ApiResponse<string>.Fail("This URL has been deactivated.", 410);
            }

            if (url.ExpiresAt.HasValue && url.ExpiresAt.Value < DateTime.UtcNow)
            {
                throw new UrlExpiredException();
            }

            if (url.ClickLimit > 0 && url.TotalClicks >= url.ClickLimit)
            {
                return ApiResponse<string>.Fail("This URL has reached its click limit.", 410);
            }

            // Record click
            _ = Task.Run(async () =>
            {
                try
                {
                    await _analyticsService.RecordClickAsync(url.Id, ipAddress, userAgent, referer, CancellationToken.None);
                    await _urlRepo.IncrementClickCountAsync(url.Id, CancellationToken.None);
                }
                catch { }
            }, CancellationToken.None);

            return ApiResponse<string>.Ok(AppendUtmParams(url));
        }

        /// <summary>
        /// Update URL metadata
        /// </summary>
        public async Task<ApiResponse<UrlResponseDto>> UpdateAsync(
            Guid id,
            string userId,
            UpdateUrlDto dto,
            CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(id, ct);
            if (url is null)
            {
                return ApiResponse<UrlResponseDto>.NotFound("URL not found.");
            }

            if (url.UserId != userId)
            {
                return ApiResponse<UrlResponseDto>.Forbidden("You don't have permission to update this URL.");
            }

            // Update fields
            if (dto.Title is not null) url.Title = dto.Title;
            if (dto.Description is not null) url.Description = dto.Description;
            if (dto.ExpiresAt.HasValue) url.ExpiresAt = dto.ExpiresAt;
            if (dto.IsActive.HasValue) url.IsActive = dto.IsActive.Value;
            if (dto.UtmSource is not null) url.UtmSource = dto.UtmSource;
            if (dto.UtmMedium is not null) url.UtmMedium = dto.UtmMedium;
            if (dto.UtmCampaign is not null) url.UtmCampaign = dto.UtmCampaign;

            if (dto.Tags is not null)
            {
                url.Tags = dto.Tags.Any() ? JsonSerializer.Serialize(dto.Tags) : null;
            }

            url.UpdatedAt = DateTime.UtcNow;

            await _urlRepo.UpdateAsync(url, ct);

            // Clear cache
            await _cache.RemoveAsync($"{CachePrefix}{url.ShortCode}", ct);

            var response = _mapper.Map<UrlResponseDto>(url);
            return ApiResponse<UrlResponseDto>.Ok(response, "URL updated successfully.");
        }

        /// <summary>
        /// Delete URL (soft delete)
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, string userId, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(id, ct);
            if (url is null)
            {
                return ApiResponse<bool>.NotFound("URL not found.");
            }

            if (url.UserId != userId)
            {
                return ApiResponse<bool>.Forbidden("You don't have permission to delete this URL.");
            }

            await _urlRepo.SoftDeleteAsync(url, ct);
            await _cache.RemoveAsync($"{CachePrefix}{url.ShortCode}", ct);

            return ApiResponse<bool>.Ok(true, "URL deleted successfully.");
        }

        /// <summary>
        /// Get paginated user URLs
        /// </summary>
        public async Task<ApiResponse<PagedResult<UrlResponseDto>>> GetUserUrlsAsync(
            string userId,
            PaginationRequest request,
            CancellationToken ct = default)
        {
            var (items, total) = await _urlRepo.GetPagedByUserIdAsync(
                userId,
                request.Page,
                request.PageSize,
                request.Search,
                ct);

            var mapped = _mapper.Map<IEnumerable<UrlResponseDto>>(items);

            var result = PagedResult<UrlResponseDto>.Create(mapped, total, request.Page, request.PageSize);
            return ApiResponse<PagedResult<UrlResponseDto>>.Ok(result);
        }

        /// <summary>
        /// Bulk create URLs
        /// </summary>
        public async Task<ApiResponse<List<UrlResponseDto>>> BulkCreateAsync(
            string userId,
            BulkCreateUrlDto dto,
            CancellationToken ct = default)
        {
            if (dto.Urls.Count > 100)
            {
                return ApiResponse<List<UrlResponseDto>>.Fail("Bulk create limit is 100 URLs at a time.", 400);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return ApiResponse<List<UrlResponseDto>>.NotFound("User not found.");
            }

            var currentCount = await _urlRepo.CountAsync(x => x.UserId == userId, ct);
            var remainingQuota = user.UrlLimit - currentCount;

            if (dto.Urls.Count > remainingQuota)
            {
                return ApiResponse<List<UrlResponseDto>>.Fail(
                    $"Bulk operation would exceed your URL limit. You can create {remainingQuota} more URLs.",
                    429);
            }

            var results = new List<UrlResponseDto>();
            var errors = new List<string>();

            foreach (var urlDto in dto.Urls)
            {
                try
                {
                    var response = await CreateAsync(userId, urlDto, ct);
                    if (response.Success && response.Data is not null)
                    {
                        results.Add(response.Data);
                    }
                    else
                    {
                        errors.Add($"Failed to create URL for {urlDto.OriginalUrl}: {response.Message}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error creating URL for {urlDto.OriginalUrl}: {ex.Message}");
                }
            }

            if (results.Count == 0)
            {
                return ApiResponse<List<UrlResponseDto>>.Fail(
                    "Failed to create any URLs.",
                    400,
                    errors);
            }

            var message = results.Count == dto.Urls.Count
                ? $"{results.Count} URLs created successfully."
                : $"{results.Count} URLs created successfully. {errors.Count} failed.";

            return ApiResponse<List<UrlResponseDto>>.Created(results, message);
        }

        /// <summary>
        /// Toggle active/inactive status
        /// </summary>
        public async Task<ApiResponse<bool>> ToggleActiveAsync(Guid id, string userId, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(id, ct);
            if (url is null)
            {
                return ApiResponse<bool>.NotFound("URL not found.");
            }

            if (url.UserId != userId)
            {
                return ApiResponse<bool>.Forbidden("Access denied.");
            }

            url.IsActive = !url.IsActive;
            url.UpdatedAt = DateTime.UtcNow;

            await _urlRepo.UpdateAsync(url, ct);
            await _cache.RemoveAsync($"{CachePrefix}{url.ShortCode}", ct);

            var message = url.IsActive ? "URL activated." : "URL deactivated.";
            return ApiResponse<bool>.Ok(url.IsActive, message);
        }

        /// <summary>
        /// Generate QR code for URL
        /// </summary>
        public async Task<ApiResponse<string>> GenerateQrCodeAsync(Guid id, CancellationToken ct = default)
        {
            var url = await _urlRepo.GetByIdAsync(id, ct);
            if (url is null)
            {
                return ApiResponse<string>.NotFound("URL not found.");
            }

            var qrCode = GenerateQrCodeBase64(url.ShortUrl);
            return ApiResponse<string>.Ok(qrCode);
        }

        /// <summary>
        /// Check if password is correct for protected URL
        /// </summary>
        public async Task<ApiResponse<bool>> CheckPasswordAsync(
            string shortCode,
            string password,
            CancellationToken ct = default)
        {
            var url = await GetUrlByCodeOrAliasAsync(shortCode, ct);
            if (url is null)
            {
                return ApiResponse<bool>.NotFound("URL not found.");
            }

            if (!url.IsPasswordProtected || url.Password is null)
            {
                return ApiResponse<bool>.Ok(true);
            }

            var valid = _tokenService.VerifyPassword(password, url.Password);

            if (!valid)
            {
                return ApiResponse<bool>.Fail("Incorrect password.", 401);
            }

            return ApiResponse<bool>.Ok(true);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Get URL by short code or custom alias
        /// </summary>
        private async Task<ShortenedUrl?> GetUrlByCodeOrAliasAsync(string code, CancellationToken ct)
        {
            var url = await _urlRepo.GetByShortCodeAsync(code, ct);
            if (url is not null) return url;

            return await _urlRepo.GetByCustomAliasAsync(code, ct);
        }

        /// <summary>
        /// Generate a unique short code
        /// </summary>
        private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken ct)
        {
            var length = int.Parse(_config["AppSettings:ShortCodeLength"] ?? "6");
            string code;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                code = _tokenService.GenerateShortCode(length);
                attempts++;

                // If too many collisions, increase length to reduce probability
                if (attempts > maxAttempts)
                {
                    length++;
                    attempts = 0;
                }

                if (length > 12)
                {
                    throw new InvalidOperationException("Unable to generate unique short code after multiple attempts.");
                }

            } while (await _urlRepo.ShortCodeExistsAsync(code, ct));

            return code;
        }

        /// <summary>
        /// Append UTM parameters to original URL if configured
        /// </summary>
        private static string AppendUtmParams(ShortenedUrl url)
        {
            var target = url.OriginalUrl;

            var hasUtm = !string.IsNullOrWhiteSpace(url.UtmSource)
                         || !string.IsNullOrWhiteSpace(url.UtmMedium)
                         || !string.IsNullOrWhiteSpace(url.UtmCampaign);

            if (!hasUtm) return target;

            var separator = target.Contains('?') ? "&" : "?";
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(url.UtmSource))
                parts.Add($"utm_source={Uri.EscapeDataString(url.UtmSource)}");

            if (!string.IsNullOrWhiteSpace(url.UtmMedium))
                parts.Add($"utm_medium={Uri.EscapeDataString(url.UtmMedium)}");

            if (!string.IsNullOrWhiteSpace(url.UtmCampaign))
                parts.Add($"utm_campaign={Uri.EscapeDataString(url.UtmCampaign)}");

            return target + separator + string.Join("&", parts);
        }

        /// <summary>
        /// Generate QR code as Base64 PNG image
        /// </summary>
        private static string GenerateQrCodeBase64(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var bytes = qrCode.GetGraphic(10);
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }

        /// <summary>
        /// Cached URL data structure
        /// </summary>
        private record CachedUrlData(string OriginalUrl, bool IsPasswordProtected);
    }

}

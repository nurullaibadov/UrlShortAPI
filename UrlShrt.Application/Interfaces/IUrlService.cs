using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Url;

namespace UrlShrt.Application.Interfaces
{
    public interface IUrlService
    {
        Task<ApiResponse<UrlResponseDto>> CreateAsync(string? userId, CreateUrlDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<UrlResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ApiResponse<UrlResponseDto>> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ResolveAsync(string shortCode, string ipAddress, string userAgent, string? referer, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ResolveWithPasswordAsync(string shortCode, string password, string ipAddress, string userAgent, string? referer, CancellationToken cancellationToken = default);
        Task<ApiResponse<UrlResponseDto>> UpdateAsync(Guid id, string userId, UpdateUrlDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<PagedResult<UrlResponseDto>>> GetUserUrlsAsync(string userId, PaginationRequest request, CancellationToken cancellationToken = default);
        Task<ApiResponse<List<UrlResponseDto>>> BulkCreateAsync(string userId, BulkCreateUrlDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> ToggleActiveAsync(Guid id, string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> GenerateQrCodeAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> CheckPasswordAsync(string shortCode, string password, CancellationToken cancellationToken = default);
    }
}

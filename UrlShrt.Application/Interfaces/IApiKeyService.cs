using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.ApiKey;

namespace UrlShrt.Application.Interfaces
{
    public interface IApiKeyService
    {
        Task<ApiResponse<ApiKeyDto>> CreateAsync(string userId, CreateApiKeyDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<List<ApiKeyDto>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> RevokeAsync(Guid id, string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
    }
}

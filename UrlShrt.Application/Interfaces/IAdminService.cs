using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Admin;
using UrlShrt.Application.DTOs.Url;

namespace UrlShrt.Application.Interfaces
{
    public interface IAdminService
    {
        Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default);
        Task<ApiResponse<AdminUserDto>> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ApiResponse<AdminUserDto>> CreateUserAsync(CreateUserByAdminDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> UpdateUserRoleAsync(UpdateUserRoleDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> UpdateUserStatusAsync(UpdateUserStatusDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> UpdateUserPlanAsync(UpdateUserPlanDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> LockUserAsync(string id, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> UnlockUserAsync(string id, CancellationToken cancellationToken = default);
        Task<ApiResponse<PagedResult<UrlResponseDto>>> GetAllUrlsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> AdminDeleteUrlAsync(Guid urlId, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> CleanExpiredUrlsAsync(CancellationToken cancellationToken = default);
    }
}

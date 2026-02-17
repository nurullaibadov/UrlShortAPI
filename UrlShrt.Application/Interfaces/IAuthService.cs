using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Auth;

namespace UrlShrt.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> ResendConfirmationEmailAsync(ResendConfirmationDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<UserDto>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> LogoutAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> RevokeTokenAsync(string userId, CancellationToken cancellationToken = default);
    }
}

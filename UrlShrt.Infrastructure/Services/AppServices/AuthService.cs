using AutoMapper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.Auth;
using UrlShrt.Application.Interfaces;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Interfaces.Services;

namespace UrlShrt.Infrastructure.Services.AppServices
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
            _mapper = mapper;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                return ApiResponse<AuthResponseDto>.Conflict("This email address is already registered.");

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Email,
                Email = dto.Email,
                IsActive = true,
                Plan = "Free",
                UrlLimit = int.Parse(_config["AppSettings:DefaultUrlLimit"] ?? "100")
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDto>.Fail("Registration failed.", 400, errors);
            }

            await _userManager.AddToRoleAsync(user, "User");

            // Email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = _config["AppSettings:BaseUrl"];
            var confirmLink = $"{baseUrl}/api/v1/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            try { await _emailService.SendConfirmationEmailAsync(user.Email!, confirmLink, ct); }
            catch { /* log but don't fail */ }

            try { await _emailService.SendWelcomeEmailAsync(user.Email!, user.FullName, ct); }
            catch { /* log but don't fail */ }

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await BuildAuthResponseAsync(user, roles);

            return ApiResponse<AuthResponseDto>.Created(authResponse, "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse<AuthResponseDto>.Unauthorized("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Unauthorized("Your account has been deactivated. Please contact support.");

            if (await _userManager.IsLockedOutAsync(user))
                return ApiResponse<AuthResponseDto>.Unauthorized("Account is locked. Try again later.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return ApiResponse<AuthResponseDto>.Unauthorized("Account locked due to too many failed attempts.");
                return ApiResponse<AuthResponseDto>.Unauthorized("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await BuildAuthResponseAsync(user, roles);

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
        {
            // Find user by refresh token
            var users = _userManager.Users.Where(u => u.RefreshToken == dto.RefreshToken).ToList();
            var user = users.FirstOrDefault();

            if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return ApiResponse<AuthResponseDto>.Unauthorized("Invalid or expired refresh token.");

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await BuildAuthResponseAsync(user, roles);

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Token refreshed.");
        }

        public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            // Always return success to prevent email enumeration
            if (user is null)
                return ApiResponse<string>.Ok("If this email exists, a reset link has been sent.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var baseUrl = _config["AppSettings:BaseUrl"];
            var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";

            try { await _emailService.SendPasswordResetEmailAsync(dto.Email, resetLink, ct); }
            catch { /* log */ }

            return ApiResponse<string>.Ok("If this email exists, a password reset link has been sent.");
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse<string>.Fail("Invalid request.", 400);

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.Fail("Password reset failed.", 400, errors);
            }

            // Revoke all refresh tokens
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Ok("Password reset successfully.");
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<string>.NotFound("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.Fail("Password change failed.", 400, errors);
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Ok("Password changed successfully.");
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null)
                return ApiResponse<string>.NotFound("User not found.");

            var result = await _userManager.ConfirmEmailAsync(user, dto.Token);
            if (!result.Succeeded)
                return ApiResponse<string>.Fail("Email confirmation failed. The link may be expired.", 400);

            return ApiResponse<string>.Ok("Email confirmed successfully.");
        }

        public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(ResendConfirmationDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse<string>.Ok("If this email exists, a confirmation link has been sent.");

            if (user.EmailConfirmed)
                return ApiResponse<string>.Fail("Email is already confirmed.", 400);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = _config["AppSettings:BaseUrl"];
            var confirmLink = $"{baseUrl}/api/v1/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            try { await _emailService.SendConfirmationEmailAsync(dto.Email, confirmLink, ct); }
            catch { /* log */ }

            return ApiResponse<string>.Ok("Confirmation email resent.");
        }

        public async Task<ApiResponse<UserDto>> GetProfileAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<UserDto>.NotFound("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserDto>(user);
            dto.Roles = roles;

            return ApiResponse<UserDto>.Ok(dto);
        }

        public async Task<ApiResponse<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<UserDto>.NotFound("User not found.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.ProfileImageUrl = dto.ProfileImageUrl;
            user.TimeZone = dto.TimeZone;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;

            return ApiResponse<UserDto>.Ok(userDto, "Profile updated successfully.");
        }

        public async Task<ApiResponse<string>> LogoutAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _userManager.UpdateAsync(user);
            }
            return ApiResponse<string>.Ok("Logged out successfully.");
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<string>.NotFound("User not found.");

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Ok("Token revoked.");
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            var accessMinutes = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "60");

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshDays);
            await _userManager.UpdateAsync(user);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(accessMinutes),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshDays),
                User = userDto
            };
        }
    }
}

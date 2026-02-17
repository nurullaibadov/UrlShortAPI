using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Application.DTOs.Auth
{
    public record RegisterDto(
      string FirstName,
      string LastName,
      string Email,
      string Password,
      string ConfirmPassword
  );

    public record LoginDto(
        string Email,
        string Password,
        bool RememberMe = false
    );

    public record ForgotPasswordDto(string Email);

    public record ResetPasswordDto(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword
    );

    public record ChangePasswordDto(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    );

    public record RefreshTokenDto(
        string AccessToken,
        string RefreshToken
    );

    public record ConfirmEmailDto(
        string UserId,
        string Token
    );

    public record ResendConfirmationDto(string Email);

    public record UpdateProfileDto(
        string FirstName,
        string LastName,
        string? ProfileImageUrl,
        string? TimeZone
    );
}

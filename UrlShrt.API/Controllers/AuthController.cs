using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UrlShrt.Application.DTOs.Auth;
using UrlShrt.Application.Interfaces;

namespace UrlShrt.API.Controllers
{
    [SwaggerTag("Authentication — Register, Login, Password Management")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<ForgotPasswordDto> _forgotValidator;
        private readonly IValidator<ResetPasswordDto> _resetValidator;
        private readonly IValidator<ChangePasswordDto> _changeValidator;

        public AuthController(
            IAuthService authService,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            IValidator<ForgotPasswordDto> forgotValidator,
            IValidator<ResetPasswordDto> resetValidator,
            IValidator<ChangePasswordDto> changeValidator)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _forgotValidator = forgotValidator;
            _resetValidator = resetValidator;
            _changeValidator = changeValidator;
        }

        /// <summary>Register a new user account</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Register", Description = "Create a new user account")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
        {
            var validation = await _registerValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.RegisterAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Login with email and password</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Login", Description = "Authenticate with email and password")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            var validation = await _loginValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.LoginAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Refresh access token using refresh token</summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Refresh Token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Request password reset email</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Forgot Password", Description = "Send password reset email")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
        {
            var validation = await _forgotValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.ForgotPasswordAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Reset password using token from email</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Reset Password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        {
            var validation = await _resetValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.ResetPasswordAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Change password (authenticated)</summary>
        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(Summary = "Change Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        {
            var validation = await _changeValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return BadRequest(new { success = false, errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.ChangePasswordAsync(CurrentUserId!, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Confirm email address</summary>
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Confirm Email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token, CancellationToken ct)
        {
            var result = await _authService.ConfirmEmailAsync(new ConfirmEmailDto(userId, token), ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Resend email confirmation</summary>
        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Resend Confirmation Email")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto dto, CancellationToken ct)
        {
            var result = await _authService.ResendConfirmationEmailAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Get current user profile</summary>
        [HttpGet("profile")]
        [Authorize]
        [SwaggerOperation(Summary = "Get Profile")]
        public async Task<IActionResult> GetProfile(CancellationToken ct)
        {
            var result = await _authService.GetProfileAsync(CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Update profile information</summary>
        [HttpPut("profile")]
        [Authorize]
        [SwaggerOperation(Summary = "Update Profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var result = await _authService.UpdateProfileAsync(CurrentUserId!, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Logout and revoke refresh token</summary>
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var result = await _authService.LogoutAsync(CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Revoke refresh token</summary>
        [HttpPost("revoke-token")]
        [Authorize]
        [SwaggerOperation(Summary = "Revoke Token")]
        public async Task<IActionResult> RevokeToken(CancellationToken ct)
        {
            var result = await _authService.RevokeTokenAsync(CurrentUserId!, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

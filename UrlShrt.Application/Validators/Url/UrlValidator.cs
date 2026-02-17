using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.DTOs.Url;

namespace UrlShrt.Application.Validators.Url
{
    public class CreateUrlValidator : AbstractValidator<CreateUrlDto>
    {
        public CreateUrlValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .NotEmpty().WithMessage("Original URL is required.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var result)
                             && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Invalid URL format. Must start with http:// or https://")
                .MaximumLength(2048).WithMessage("URL must not exceed 2048 characters.");

            RuleFor(x => x.CustomAlias)
                .MaximumLength(50).WithMessage("Custom alias must not exceed 50 characters.")
                .Matches("^[a-zA-Z0-9_-]+$").When(x => x.CustomAlias is not null)
                .WithMessage("Custom alias can only contain letters, numbers, hyphens, and underscores.");

            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt.HasValue)
                .WithMessage("Expiration date must be in the future.");

            RuleFor(x => x.ClickLimit)
                .GreaterThanOrEqualTo(0).WithMessage("Click limit must be 0 or greater. 0 means unlimited.");

            RuleFor(x => x.Password)
                .MinimumLength(4).When(x => x.Password is not null)
                .WithMessage("Password must be at least 4 characters.");
        }
    }

    public class UpdateUrlValidator : AbstractValidator<UpdateUrlDto>
    {
        public UpdateUrlValidator()
        {
            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt.HasValue)
                .WithMessage("Expiration date must be in the future.");
        }
    }
}

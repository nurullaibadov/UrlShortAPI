using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public string Code { get; }
        public DomainException(string message, string code = "DOMAIN_ERROR") : base(message)
        {
            Code = code;
        }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string entity, object key)
            : base($"{entity} with key '{key}' was not found.", "NOT_FOUND") { }
    }

    public class ValidationException : DomainException
    {
        public IDictionary<string, string[]> Errors { get; }
        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", "VALIDATION_ERROR")
        {
            Errors = errors;
        }
    }

    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message = "Unauthorized access.")
            : base(message, "UNAUTHORIZED") { }
    }

    public class ConflictException : DomainException
    {
        public ConflictException(string message)
            : base(message, "CONFLICT") { }
    }

    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message = "Access forbidden.")
            : base(message, "FORBIDDEN") { }
    }

    public class UrlExpiredException : DomainException
    {
        public UrlExpiredException() : base("This URL has expired.", "URL_EXPIRED") { }
    }

    public class UrlLimitExceededException : DomainException
    {
        public UrlLimitExceededException() : base("URL creation limit exceeded for your plan.", "URL_LIMIT_EXCEEDED") { }
    }

    public class PasswordRequiredException : DomainException
    {
        public PasswordRequiredException() : base("This URL is password protected.", "PASSWORD_REQUIRED") { }
    }
}

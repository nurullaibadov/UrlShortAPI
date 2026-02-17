using System.Net;
using System.Text.Json;
using UrlShrt.Application.Common.Models;
using UrlShrt.Domain.Exceptions;

namespace UrlShrt.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, errors) = exception switch
            {
                NotFoundException ex => (HttpStatusCode.NotFound, ex.Message, (List<string>?)null),
                ValidationException ex => (HttpStatusCode.BadRequest, ex.Message, ex.Errors.SelectMany(e => e.Value).ToList()),
                UnauthorizedException ex => (HttpStatusCode.Unauthorized, ex.Message, null),
                ForbiddenException ex => (HttpStatusCode.Forbidden, ex.Message, null),
                ConflictException ex => (HttpStatusCode.Conflict, ex.Message, null),
                UrlExpiredException ex => (HttpStatusCode.Gone, ex.Message, null),
                UrlLimitExceededException ex => (HttpStatusCode.TooManyRequests, ex.Message, null),
                DomainException ex => (HttpStatusCode.BadRequest, ex.Message, null),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred.", null)
            };

            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(message, (int)statusCode, errors);
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}

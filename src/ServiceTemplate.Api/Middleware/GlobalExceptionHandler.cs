using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceTemplate.Application.Common.Exceptions;
using ServiceTemplate.Domain.Exceptions;

namespace ServiceTemplate.Api.Middleware;

/// <summary>
/// Global exception handler generating RFC 7807 ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        httpContext.Response.ContentType = "application/problem+json";

        switch (exception)
        {
            case ValidationException validationException:
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                var problemDetails = new HttpValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = "See the errors field for details.",
                    Instance = httpContext.Request.Path
                };

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                return true;
            }

            case OrderNotFoundException notFoundException:
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Order Not Found",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Detail = notFoundException.Message,
                    Instance = httpContext.Request.Path
                };

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                return true;
            }

            case DomainException domainException:
            {
                httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business Rule Violation",
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Detail = domainException.Message,
                    Instance = httpContext.Request.Path
                };

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                return true;
            }

            default:
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : "An unexpected internal server error occurred.",
                    Instance = httpContext.Request.Path
                };

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                return true;
            }
        }
    }
}

using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Middleware;

public sealed record ErrorResponse(string Code, string Message);

/// <summary>Turns business exceptions into { code, message } JSON with the right status; anything else becomes a generic 500.</summary>
public sealed class ExceptionHandlingMiddleware
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
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            int status;
            ErrorResponse body;

            switch (ex)
            {
                case DomainException domain:
                    status = domain switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        ValidationException => StatusCodes.Status400BadRequest,
                        UnauthorizedException => StatusCodes.Status401Unauthorized,
                        ForbiddenException => StatusCodes.Status403Forbidden,
                        ConflictException => StatusCodes.Status409Conflict,
                        _ => StatusCodes.Status400BadRequest
                    };
                    body = new ErrorResponse(domain.Code, domain.Message);
                    break;

                case BadHttpRequestException:
                    status = StatusCodes.Status400BadRequest;
                    body = new ErrorResponse("VALIDATION_ERROR", "The request is invalid.");
                    break;

                default:
                    _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                    status = StatusCodes.Status500InternalServerError;
                    body = new ErrorResponse("INTERNAL_ERROR", "Something went wrong on the server.");
                    break;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(body);
        }
    }
}

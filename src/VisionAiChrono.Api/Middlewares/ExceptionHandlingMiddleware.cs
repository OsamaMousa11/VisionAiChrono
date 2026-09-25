using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ValidationException = CleanArchitectureTemplate_Application.Exceptions.ValidationException;

namespace VisionAiChrono.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Global Exception caught: {Message}", exception.Message);

        // ·Ê «·‹ Response »œ√ Ì »⁄  Œ·«’° „Ì‰›⁄‘ ‰⁄œ· «·‹ StatusCode √Ê «·‹ Headers
        // ( ⁄œÌ·Â„ Â‰« ÂÌ—„Ì InvalidOperationException ÊÌﬂ”— «·‹ middleware ‰›”Â)
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "The response has already started, the exception middleware could not update the response.");
            return;
        }

        var statusCode = GetStatusCode(exception);
        var errorName = GetErrorName(statusCode);

        var message = (statusCode == StatusCodes.Status500InternalServerError && !_env.IsDevelopment())
            ? "An unexpected error occurred on the server. Please try again later."
            : exception.Message;

        // œ⁄„ ≈—Ã«⁄ √Œÿ«¡ «·‹ Validation ≈‰ ÊÃœ  (Â ŸÂ— ›ﬁÿ ·Ê ValidationException)
        IReadOnlyDictionary<string, string[]>? validationErrors = exception switch
        {
            ValidationException valEx => valEx.Errors,
            _ => null
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = statusCode,
            error = errorName,
            message,
            errors = validationErrors,
            traceId = context.TraceIdentifier // „›Ìœ ··  »⁄/«·œ⁄„ «·›‰Ì
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static int GetStatusCode(Exception exception)
        => exception switch
        {
            BadRequestException or ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException or UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ExternalServiceException => StatusCodes.Status502BadGateway, // √Ê 500 Õ”» ÿ»Ì⁄… «·Œœ„… «·Œ«—ÃÌ…
            AppException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetErrorName(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status400BadRequest => "BadRequest",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "NotFound",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status502BadGateway => "BadGateway",
            StatusCodes.Status500InternalServerError => "InternalServerError",
            _ => "Error"
        };
}
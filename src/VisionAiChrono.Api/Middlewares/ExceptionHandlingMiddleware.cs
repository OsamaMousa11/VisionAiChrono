
using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ValidationException = CleanArchitectureTemplate_Application.Exceptions.ValidationException;

namespace CleanArchitectureTemplate_Api.Middlewares;

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
        var statusCode = GetStatusCode(exception);
        var errorName = GetErrorName(statusCode);
        var message = (statusCode == StatusCodes.Status500InternalServerError && !_env.IsDevelopment())
            ? "An unexpected error occurred on the server. Please try again later."
            : exception.Message;

        _logger.LogError(exception, "Global Exception: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        if (!context.Response.HasStarted)
        {
            var response = new
            {
                status = statusCode,
                error = errorName,
                message = message
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

    private static int GetStatusCode(Exception exception)
        => exception switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetErrorName(int statusCode)
        => statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            409 => "Conflict",
            500 => "InternalServerError",
            _ => "Error"
        };
}
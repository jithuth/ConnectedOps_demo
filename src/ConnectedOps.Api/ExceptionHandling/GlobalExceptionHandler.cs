using System.Diagnostics;
using System.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request cancelled by client. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = 499;

            return true;
        }

        var (statusCode, title) = MapException(exception);

        LogException(
            exception,
            statusCode,
            httpContext);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = GetSafeDetail(
                exception,
                statusCode),
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        problemDetails.Extensions["timestampUtc"] =
            DateTime.UtcNow;

        httpContext.Response.StatusCode =
            statusCode;

        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static (
        int StatusCode,
        string Title)
        MapException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException =>
                (
                    StatusCodes.Status404NotFound,
                    "Resource not found"
                ),

            ArgumentException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Invalid request"
                ),

            UnauthorizedAccessException =>
                (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized"
                ),

            SecurityException =>
                (
                    StatusCodes.Status403Forbidden,
                    "Forbidden"
                ),

            DbUpdateException =>
                (
                    StatusCodes.Status409Conflict,
                    "Database operation conflict"
                ),

            InvalidOperationException =>
                (
                    StatusCodes.Status409Conflict,
                    "Operation conflict"
                ),

            BadHttpRequestException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Invalid HTTP request"
                ),

            _ =>
                (
                    StatusCodes.Status500InternalServerError,
                    "Internal server error"
                )
        };
    }

    private static string GetSafeDetail(
        Exception exception,
        int statusCode)
    {
        if (exception is DbUpdateException)
        {
            return
                "The requested database operation could not be completed.";
        }

        if (statusCode ==
            StatusCodes.Status500InternalServerError)
        {
            return
                "An unexpected error occurred while processing the request.";
        }

        return exception.Message;
    }

    private void LogException(
        Exception exception,
        int statusCode,
        HttpContext httpContext)
    {
        var traceId =
            Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);

            return;
        }

        _logger.LogWarning(
            exception,
            "Request failed. StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, TraceId: {TraceId}",
            statusCode,
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);
    }
}
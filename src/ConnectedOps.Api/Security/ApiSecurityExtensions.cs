using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

namespace ConnectedOps.Api.Security;

public static class ApiSecurityExtensions
{
    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services)
    {
        AddRateLimiting(services);
        AddRequestLimits(services);

        return services;
    }

    private static void AddRateLimiting(
        IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            // ------------------------------------------------
            // Global protection
            // ------------------------------------------------

            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(
                    context =>
                    {
                        var partitionKey =
                            GetClientPartitionKey(context);

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                partitionKey,
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit = 300,

                                        Window =
                                            TimeSpan.FromMinutes(1),

                                        QueueLimit = 0,

                                        AutoReplenishment = true
                                    });
                    });

            // ------------------------------------------------
            // Authentication endpoints
            //
            // Much stricter than general API traffic.
            // ------------------------------------------------

            options.AddPolicy(
                "authentication",
                context =>
                {
                    var partitionKey =
                        GetClientPartitionKey(context);

                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            partitionKey,
                            _ =>
                                new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 10,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    QueueLimit = 0,

                                    AutoReplenishment = true
                                });
                });

            // ------------------------------------------------
            // Sensitive operations
            // ------------------------------------------------

            options.AddPolicy(
                "sensitive",
                context =>
                {
                    var partitionKey =
                        GetClientPartitionKey(context);

                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            partitionKey,
                            _ =>
                                new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 30,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    QueueLimit = 0,

                                    AutoReplenishment = true
                                });
                });

            options.OnRejected =
                async (context, cancellationToken) =>
                {
                    context.HttpContext
                        .Response
                        .ContentType =
                        "application/problem+json";

                    await context.HttpContext
                        .Response
                        .WriteAsJsonAsync(
                            new
                            {
                                type =
                                    "https://httpstatuses.com/429",

                                title =
                                    "Too many requests",

                                status =
                                    StatusCodes
                                        .Status429TooManyRequests,

                                detail =
                                    "Too many requests were received. Please try again later.",

                                traceId =
                                    context.HttpContext
                                        .TraceIdentifier
                            },
                            cancellationToken);
                };
        });
    }

    private static void AddRequestLimits(
        IServiceCollection services)
    {
        services.Configure<FormOptions>(
            options =>
            {
                // 10 MB.
                //
                // Future file-upload endpoints should define their
                // own explicit limits instead of raising this globally.
                options.MultipartBodyLengthLimit =
                    10 * 1024 * 1024;
            });
    }

    private static string GetClientPartitionKey(
        HttpContext context)
    {
        // Prefer authenticated user ID.
        var userId =
            context.User
                .FindFirst("user_id")?
                .Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        // Before authentication we fall back to source IP.
        var ipAddress =
            context.Connection
                .RemoteIpAddress?
                .ToString();

        return $"ip:{ipAddress ?? "unknown"}";
    }
}
using System.Diagnostics;
using System.Text.Json;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConnectedOps.Api.Diagnostics;

public static class HealthCheckExtensions
{
    private static readonly DateTime ProcessStartTime = DateTime.UtcNow;

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API process is running and responsive."), tags: ["live"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "db"])
            .AddCheck<MemoryHealthCheck>("memory", tags: ["ready", "system"]);

        return services;
    }

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe (e.g. Kubernetes, AWS ALB / ECS)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponseAsync
        });

        // Readiness probe (checks DB connectivity)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponseAsync
        });

        // Detailed full health status
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteDetailedHealthResponseAsync
        });

        return endpoints;
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds
        });

        return context.Response.WriteAsync(result);
    }

    private static Task WriteDetailedHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            version = "1.23.0",
            dotnetVersion = Environment.Version.ToString(),
            uptime = (DateTime.UtcNow - ProcessStartTime).ToString(@"dd\.hh\:mm\:ss"),
            serverTimeUtc = DateTime.UtcNow,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        return context.Response.WriteAsync(json);
    }
}

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ConnectedOpsDbContext _dbContext;

    public DatabaseHealthCheck(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            sw.Stop();

            if (canConnect)
            {
                return HealthCheckResult.Healthy(
                    $"Database connectivity OK (latency: {sw.ElapsedMilliseconds} ms)",
                    data: new Dictionary<string, object>
                    {
                        ["provider"] = "Microsoft.EntityFrameworkCore.SqlServer",
                        ["latencyMs"] = sw.ElapsedMilliseconds
                    });
            }

            return HealthCheckResult.Unhealthy("Cannot connect to SQL Server database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database connection failed: {ex.Message}");
        }
    }
}

public sealed class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(false);
        var allocatedMb = allocatedBytes / (1024 * 1024);

        // Warning threshold: 1 GB
        if (allocatedMb > 1024)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Process memory high: {allocatedMb} MB",
                data: new Dictionary<string, object> { ["allocatedMb"] = allocatedMb }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory OK: {allocatedMb} MB allocated",
            data: new Dictionary<string, object>
            {
                ["allocatedMb"] = allocatedMb,
                ["gen0Collections"] = GC.CollectionCount(0),
                ["gen1Collections"] = GC.CollectionCount(1),
                ["gen2Collections"] = GC.CollectionCount(2)
            }));
    }
}

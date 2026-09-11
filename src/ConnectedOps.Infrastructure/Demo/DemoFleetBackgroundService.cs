using ConnectedOps.Application.Demo;
using ConnectedOps.Application.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Demo;

public sealed class DemoFleetBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DemoFleetSettings _settings;
    private readonly ILogger<DemoFleetBackgroundService> _logger;

    public DemoFleetBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<DemoFleetSettings> settings,
        ILogger<DemoFleetBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Demo fleet background service is disabled in configuration.");
            return;
        }

        _logger.LogInformation("Demo fleet background service started. Interval: {Interval}s", _settings.UpdateIntervalSeconds);

        int intervalMs = Math.Max(1000, _settings.UpdateIntervalSeconds * 1000);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var simulator = scope.ServiceProvider.GetRequiredService<IDemoFleetSimulator>();
                var status = await simulator.GetStatusAsync(stoppingToken);

                if (status.IsRunning)
                {
                    await simulator.StepSimulationAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in demo fleet background simulation tick.");
            }

            try
            {
                await Task.Delay(intervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Demo fleet background service stopped.");
    }
}

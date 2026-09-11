using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Telematics;

[Authorize]
public sealed class HealthModel : PageModel
{
    private readonly IDeviceHealthService _healthService;
    private readonly ITrackingProviderService _providerService;

    public HealthModel(
        IDeviceHealthService healthService,
        ITrackingProviderService providerService)
    {
        _healthService = healthService;
        _providerService = providerService;
    }

    public IReadOnlyCollection<DeviceHealthDto> Devices { get; private set; } = [];
    public IReadOnlyCollection<TrackingProviderDto> Providers { get; private set; } = [];
    public int TotalCount { get; private set; }

    // KPI counters
    public int HealthyCount => Devices.Count(d => d.HealthState == DeviceHealthState.Healthy);
    public int WarningCount => Devices.Count(d => d.HealthState == DeviceHealthState.Warning);
    public int OfflineCount => Devices.Count(d => d.HealthState == DeviceHealthState.Offline);
    public int FaultedCount => Devices.Count(d => d.HealthState == DeviceHealthState.Faulted);

    [BindProperty(SupportsGet = true)]
    public DeviceHealthQueryParameters Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Devices = await _healthService.GetDeviceHealthPagedAsync(Filter, ct);
        TotalCount = await _healthService.GetDeviceHealthCountAsync(Filter, ct);
        Providers = await _providerService.GetProvidersAsync(ct);
    }
}

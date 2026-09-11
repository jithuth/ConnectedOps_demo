using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Telematics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Telematics;

[Authorize]
public sealed class LiveModel : PageModel
{
    private readonly IVehicleTelemetryStateService _stateService;
    private readonly IBranchService _branchService;

    public LiveModel(
        IVehicleTelemetryStateService stateService,
        IBranchService branchService)
    {
        _stateService = stateService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<LiveVehicleTrackingDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public LiveTrackingQueryParameters Filter { get; set; } = new();

    // Computed KPIs
    public int TotalTracked => Vehicles.Count;
    public int MovingCount => Vehicles.Count(v => v.SpeedKph > 0);
    public int StoppedCount => Vehicles.Count(v => v.SpeedKph == 0 || !v.SpeedKph.HasValue);
    public int IgnitionOnCount => Vehicles.Count(v => v.IgnitionOn == true);
    public int OnlineCount => Vehicles.Count(v => v.ConnectivityStatus == Domain.Telematics.DeviceConnectivityStatus.Online);
    public int OfflineCount => Vehicles.Count(v => v.ConnectivityStatus == Domain.Telematics.DeviceConnectivityStatus.Offline);

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Vehicles = await _stateService.GetLiveFleetTrackingAsync(Filter, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
    }
}

using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Telematics;

[Authorize]
public sealed class DevicesModel : PageModel
{
    private readonly ITrackingDeviceService _deviceService;
    private readonly ITrackingProviderService _providerService;

    public DevicesModel(
        ITrackingDeviceService deviceService,
        ITrackingProviderService providerService)
    {
        _deviceService = deviceService;
        _providerService = providerService;
    }

    public IReadOnlyCollection<TrackingDeviceListItemDto> Devices { get; private set; } = [];
    public IReadOnlyCollection<TrackingProviderDto> Providers { get; private set; } = [];
    public IReadOnlyCollection<TrackingDeviceTypeDto> DeviceTypes { get; private set; } = [];
    public int TotalCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public TrackingDeviceQueryParameters Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Devices = await _deviceService.GetDevicesPagedAsync(Filter, ct);
        TotalCount = await _deviceService.GetDeviceCountAsync(Filter, ct);
        Providers = await _providerService.GetProvidersAsync(ct);
        DeviceTypes = await _providerService.GetDeviceTypesAsync(ct);
    }
}

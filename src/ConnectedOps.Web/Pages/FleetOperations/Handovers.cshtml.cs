using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class HandoversModel : PageModel
{
    private readonly IVehicleHandoverService _handoverService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly ILocationService _locationService;

    public HandoversModel(
        IVehicleHandoverService handoverService,
        IVehicleService vehicleService,
        IDriverService driverService,
        ILocationService locationService)
    {
        _handoverService = handoverService;
        _vehicleService = vehicleService;
        _driverService = driverService;
        _locationService = locationService;
    }

    public IReadOnlyCollection<VehicleHandoverDto> Handovers { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<LocationListItemDto> Locations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DriverId { get; set; }

    [BindProperty]
    public CreateVehicleHandoverRequest HandoverInput { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        0,
        OdometerUnit.Kilometers,
        null,
        null,
        null,
        VehicleCondition.Good,
        null,
        true,
        true);

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateHandoverAsync()
    {
        try
        {
            var handover = await _handoverService.CreateHandoverAsync(HandoverInput, HttpContext.RequestAborted);
            StatusMessage = $"Vehicle '{handover.VehicleNumber}' successfully handed over to driver '{handover.ToDriverName}' at odometer {handover.Odometer:N0}.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        Handovers = await _handoverService.GetHandoversAsync(VehicleId, DriverId, ct);

        var vPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100 }, ct);
        Vehicles = vPaged.Items;

        var dPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100 }, ct);
        Drivers = dPaged.Items;

        Locations = await _locationService.GetLocationsAsync(null, ct);
    }
}

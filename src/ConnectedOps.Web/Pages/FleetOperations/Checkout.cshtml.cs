using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class CheckoutModel : PageModel
{
    private readonly IVehicleUsageSessionService _sessionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly IDriverEligibilityService _eligibilityService;
    private readonly ILocationService _locationService;

    public CheckoutModel(
        IVehicleUsageSessionService sessionService,
        IVehicleService vehicleService,
        IDriverService driverService,
        IDriverEligibilityService eligibilityService,
        ILocationService locationService)
    {
        _sessionService = sessionService;
        _vehicleService = vehicleService;
        _driverService = driverService;
        _eligibilityService = eligibilityService;
        _locationService = locationService;
    }

    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<VehicleUsageSessionDto> OpenSessions { get; private set; } = [];
    public IReadOnlyCollection<LocationListItemDto> Locations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DriverId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SessionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty]
    public CreateCheckoutRequest CheckoutInput { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        0,
        OdometerUnit.Kilometers,
        null,
        null,
        null,
        null,
        VehicleCondition.Good,
        null,
        null,
        null);

    [BindProperty]
    public CheckInSessionRequest CheckInInput { get; set; } = new(
        0,
        null,
        VehicleCondition.Good,
        null,
        null);

    [BindProperty]
    public Guid CheckInSessionId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnGetCheckEligibilityAsync(Guid driverId, Guid vehicleId)
    {
        var result = await _eligibilityService.EvaluateAsync(driverId, vehicleId, HttpContext.RequestAborted);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnGetVehicleDetailsAsync(Guid vehicleId)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId, HttpContext.RequestAborted);
        if (vehicle is null) return NotFound();
        return new JsonResult(new
        {
            odometer = vehicle.CurrentOdometer,
            unit = vehicle.OdometerUnit.ToString()
        });
    }

    public async Task<IActionResult> OnGetSessionDetailsAsync(Guid sessionId)
    {
        var session = await _sessionService.GetSessionByIdAsync(sessionId, HttpContext.RequestAborted);
        return new JsonResult(new
        {
            vehicleId = session.VehicleId,
            vehicleNumber = session.VehicleNumber,
            driverId = session.DriverId,
            driverName = session.DriverDisplayName,
            startOdometer = session.StartOdometer,
            odometerUnit = session.OdometerUnit.ToString(),
            checkedOutAt = session.CheckedOutAtUtc.ToString("g")
        });
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        try
        {
            var session = await _sessionService.CheckoutVehicleAsync(CheckoutInput, HttpContext.RequestAborted);
            StatusMessage = $"Vehicle '{session.VehicleNumber}' successfully checked out to driver '{session.DriverDisplayName}' at odometer {session.StartOdometer:N0}.";
            return RedirectToPage("/FleetOperations/Board");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDropdownsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCheckInAsync()
    {
        try
        {
            var session = await _sessionService.CheckInVehicleAsync(CheckInSessionId, CheckInInput, HttpContext.RequestAborted);
            StatusMessage = $"Vehicle '{session.VehicleNumber}' successfully returned by driver '{session.DriverDisplayName}' at odometer {session.EndOdometer:N0} (Distance: {session.DistanceTraveled:0.##} {session.OdometerUnit}).";
            return RedirectToPage("/FleetOperations/Board");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Action = "checkin";
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var ct = HttpContext.RequestAborted;
        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100, Status = VehicleStatus.Active }, ct);
        Vehicles = vehiclesPaged.Items;

        var driversPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100, Status = DriverStatus.Active }, ct);
        Drivers = driversPaged.Items;

        var openSessionsPaged = await _sessionService.GetSessionsPagedAsync(new UsageSessionQueryParameters { Status = UsageSessionStatus.Open, PageSize = 100 }, ct);
        OpenSessions = openSessionsPaged.Items;

        Locations = await _locationService.GetLocationsAsync(null, ct);
    }
}

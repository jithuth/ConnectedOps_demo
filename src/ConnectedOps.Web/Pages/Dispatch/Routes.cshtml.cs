using ConnectedOps.Application.Dispatch;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Dispatch;

[Authorize]
public class RoutesModel : PageModel
{
    private readonly IDispatchService _dispatchService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public RoutesModel(
        IDispatchService dispatchService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _dispatchService = dispatchService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    [BindProperty(SupportsGet = true)]
    public DispatchRouteStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<DispatchRouteDto> Routes { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [BindProperty]
    public CreateRouteInput CreateInput { get; set; } = new();

    public class CreateRouteInput
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly ScheduledDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public Guid? VehicleId { get; set; }
        public Guid? DriverId { get; set; }
        public string? Notes { get; set; }
    }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDropdownsAsync(cancellationToken);

        var filter = new DispatchRouteFilterRequest
        {
            SearchTerm = SearchTerm,
            Status = Status,
            VehicleId = VehicleId,
            PageNumber = PageNumber,
            PageSize = 12
        };

        Routes = await _dispatchService.GetRoutesPagedAsync(filter, cancellationToken);
    }

    private async Task LoadDropdownsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100 }, cancellationToken);
            Vehicles = vResult.Items;
        }
        catch
        {
            Vehicles = [];
        }

        try
        {
            var dResult = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100 }, cancellationToken);
            Drivers = dResult.Items;
        }
        catch
        {
            Drivers = [];
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CreateInput.Name))
        {
            ErrorMessage = "Route name is required.";
            return RedirectToPage();
        }

        try
        {
            var req = new CreateDispatchRouteRequest
            {
                Name = CreateInput.Name.Trim(),
                ScheduledDate = CreateInput.ScheduledDate,
                VehicleId = CreateInput.VehicleId,
                DriverId = CreateInput.DriverId,
                Notes = CreateInput.Notes?.Trim()
            };

            var created = await _dispatchService.CreateRouteAsync(req, cancellationToken);
            SuccessMessage = $"Route {created.RouteNumber} created successfully. You can now add stops and sequence them.";
            return RedirectToPage("/Dispatch/RouteDetails", new { id = created.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create route: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostStartAsync(Guid routeId, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.StartRouteAsync(routeId, cancellationToken);
            SuccessMessage = "Route has been marked In Progress and dispatched.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start route: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid routeId, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.CompleteRouteAsync(routeId, null, cancellationToken);
            SuccessMessage = "Route has been marked as Completed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to complete route: {ex.Message}";
        }

        return RedirectToPage();
    }
}

using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.FleetOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class ExceptionsModel : PageModel
{
    private readonly IFleetOperationalExceptionService _exceptionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public ExceptionsModel(
        IFleetOperationalExceptionService exceptionService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _exceptionService = exceptionService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    public PagedResult<FleetOperationalExceptionDto> Exceptions { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public OperationalExceptionQueryParameters Query { get; set; } = new();

    [BindProperty]
    public CreateOperationalExceptionRequest CreateInput { get; set; } = new(
        OperationalExceptionType.VehicleConditionIssue,
        OperationalExceptionSeverity.High,
        string.Empty,
        null,
        null,
        null,
        null);

    [BindProperty]
    public ResolveOperationalExceptionRequest ResolveInput { get; set; } = new(string.Empty);

    [BindProperty]
    public DismissOperationalExceptionRequest DismissInput { get; set; } = new(null);

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateExceptionAsync()
    {
        try
        {
            var ex = await _exceptionService.CreateExceptionAsync(CreateInput, HttpContext.RequestAborted);
            StatusMessage = "Operational exception reported.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResolveExceptionAsync(Guid id)
    {
        try
        {
            await _exceptionService.ResolveExceptionAsync(id, ResolveInput, HttpContext.RequestAborted);
            StatusMessage = "Operational exception marked as resolved.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDismissExceptionAsync(Guid id)
    {
        try
        {
            await _exceptionService.DismissExceptionAsync(id, DismissInput, HttpContext.RequestAborted);
            StatusMessage = "Operational exception dismissed.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    private async Task LoadDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        Exceptions = await _exceptionService.GetExceptionsPagedAsync(Query, ct);

        var vPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100 }, ct);
        Vehicles = vPaged.Items;

        var dPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100 }, ct);
        Drivers = dPaged.Items;
    }
}

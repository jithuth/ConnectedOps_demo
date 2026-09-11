using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class ShiftAssignmentsModel : PageModel
{
    private readonly IFleetShiftAssignmentService _assignmentService;
    private readonly IFleetShiftService _shiftService;
    private readonly IDriverService _driverService;
    private readonly IVehicleService _vehicleService;

    public ShiftAssignmentsModel(
        IFleetShiftAssignmentService assignmentService,
        IFleetShiftService shiftService,
        IDriverService driverService,
        IVehicleService vehicleService)
    {
        _assignmentService = assignmentService;
        _shiftService = shiftService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    public IReadOnlyCollection<FleetShiftAssignmentDto> Assignments { get; private set; } = [];
    public IReadOnlyCollection<FleetShiftDto> Shifts { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ShiftId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DriverId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty]
    public CreateShiftAssignmentRequest CreateInput { get; set; } = new(
        Guid.Empty,
        DateOnly.FromDateTime(DateTime.UtcNow),
        DateTime.UtcNow,
        null,
        null,
        null,
        null);

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateAssignmentAsync()
    {
        try
        {
            var assignment = await _assignmentService.CreateAssignmentAsync(CreateInput, HttpContext.RequestAborted);
            StatusMessage = $"Shift assignment for {assignment.AssignmentDate:yyyy-MM-dd} created successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostActivateAssignmentAsync(Guid id)
    {
        try
        {
            await _assignmentService.ActivateAssignmentAsync(id, HttpContext.RequestAborted);
            StatusMessage = "Shift assignment activated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAssignmentAsync(Guid id)
    {
        try
        {
            await _assignmentService.CompleteAssignmentAsync(id, HttpContext.RequestAborted);
            StatusMessage = "Shift assignment completed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAssignmentAsync(Guid id, string? reason)
    {
        try
        {
            await _assignmentService.CancelAssignmentAsync(id, reason, HttpContext.RequestAborted);
            StatusMessage = "Shift assignment cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        Assignments = await _assignmentService.GetAssignmentsAsync(Date, ShiftId, DriverId, VehicleId, ct);
        Shifts = await _shiftService.GetShiftsAsync(null, true, ct);

        var dPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100 }, ct);
        Drivers = dPaged.Items;

        var vPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100 }, ct);
        Vehicles = vPaged.Items;
    }
}

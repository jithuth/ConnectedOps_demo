using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class AssignmentsModel : PageModel
{
    private readonly IDriverAssignmentService _assignmentService;
    private readonly IDriverEligibilityService _eligibilityService;
    private readonly IDriverService _driverService;
    private readonly IVehicleService _vehicleService;

    public AssignmentsModel(
        IDriverAssignmentService assignmentService,
        IDriverEligibilityService eligibilityService,
        IDriverService driverService,
        IVehicleService vehicleService)
    {
        _assignmentService = assignmentService;
        _eligibilityService = eligibilityService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    public IReadOnlyCollection<DriverVehicleAssignmentDto> ActiveAssignments { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> AvailableDrivers { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> AvailableVehicles { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? PreselectedDriverId { get; set; }

    [BindProperty]
    public CreateDriverVehicleAssignmentRequest AssignmentInput { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        AssignmentType.Primary,
        DateTime.UtcNow,
        null,
        true,
        null,
        null);

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnGetCheckEligibilityAsync(Guid driverId, Guid vehicleId)
    {
        var result = await _eligibilityService.EvaluateAsync(driverId, vehicleId, HttpContext.RequestAborted);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostCreateAssignmentAsync()
    {
        try
        {
            // Evaluate eligibility first
            var eligibility = await _eligibilityService.EvaluateAsync(
                AssignmentInput.DriverId,
                AssignmentInput.VehicleId,
                HttpContext.RequestAborted);

            if (!eligibility.IsEligible)
            {
                TempData["Error"] = $"Assignment Ineligible: {string.Join("; ", eligibility.Reasons)}";
                await LoadDataAsync();
                return Page();
            }

            await _assignmentService.CreateAssignmentAsync(AssignmentInput, HttpContext.RequestAborted);
            TempData["Feedback"] = "Vehicle assignment created successfully.";
            return RedirectToPage("/Drivers/Assignments");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEndAssignmentAsync(Guid assignmentId, string? notes)
    {
        try
        {
            await _assignmentService.EndAssignmentAsync(
                assignmentId,
                new EndDriverVehicleAssignmentRequest(notes, DateTime.UtcNow),
                HttpContext.RequestAborted);

            TempData["Feedback"] = "Vehicle assignment ended successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("/Drivers/Assignments");
    }

    private async Task LoadDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        ActiveAssignments = await _assignmentService.GetAllActiveAssignmentsAsync(ct);

        var driversPaged = await _driverService.GetDriversPagedAsync(
            new DriverQueryParameters { PageSize = 100, IsActive = true, Status = DriverStatus.Active },
            ct);
        AvailableDrivers = driversPaged.Items;

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(
            new VehicleQueryParameters { PageSize = 100, IsActive = true },
            ct);
        AvailableVehicles = vehiclesPaged.Items;

        if (PreselectedDriverId.HasValue && AssignmentInput.DriverId == Guid.Empty)
        {
            AssignmentInput = AssignmentInput with { DriverId = PreselectedDriverId.Value };
        }
    }
}

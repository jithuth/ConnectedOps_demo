using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Plans;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMaintenancePlanService _planService;
    private readonly IMaintenanceServiceTypeService _serviceTypeService;
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleCategoryService _categoryService;

    public DetailsModel(
        IMaintenancePlanService planService,
        IMaintenanceServiceTypeService serviceTypeService,
        IVehicleService vehicleService,
        IVehicleCategoryService categoryService)
    {
        _planService = planService;
        _serviceTypeService = serviceTypeService;
        _vehicleService = vehicleService;
        _categoryService = categoryService;
    }

    public MaintenancePlanDto Plan { get; private set; } = null!;
    public IReadOnlyCollection<MaintenanceServiceTypeDto> ServiceTypes { get; private set; } = [];
    public IReadOnlyCollection<VehicleMaintenancePlanAssignmentDto> Assignments { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Plan = await _planService.GetByIdAsync(id, HttpContext.RequestAborted);
            ServiceTypes = await _serviceTypeService.GetAllAsync(true, HttpContext.RequestAborted);
            Assignments = await _planService.GetVehicleAssignmentsAsync(planId: id, cancellationToken: HttpContext.RequestAborted);
            
            var vehiclesResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 1000 }, HttpContext.RequestAborted);
            Vehicles = vehiclesResult.Items;
            Categories = await _categoryService.GetCategoriesAsync(cancellationToken: HttpContext.RequestAborted);

            return Page();
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = $"Maintenance plan '{id}' was not found.";
            return RedirectToPage("/Maintenance/Plans/Index");
        }
    }

    public async Task<IActionResult> OnPostUpdatePlanAsync(
        Guid id,
        string code,
        string name,
        string? description,
        Guid? vehicleCategoryId,
        bool isActive)
    {
        try
        {
            var req = new UpdateMaintenancePlanRequest(code, name, description, vehicleCategoryId, isActive);
            await _planService.UpdateAsync(id, req, HttpContext.RequestAborted);
            SuccessMessage = "Maintenance plan updated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddRuleAsync(
        Guid id,
        Guid maintenanceServiceTypeId,
        MaintenanceScheduleType scheduleType,
        decimal? intervalKilometers,
        decimal? intervalMiles,
        decimal? intervalEngineHours,
        int? intervalDays,
        int? intervalMonths,
        decimal? initialDueKilometers,
        decimal? initialDueEngineHours,
        DateTime? initialDueDateUtc,
        decimal? reminderBeforeKilometers,
        decimal? reminderBeforeEngineHours,
        int? reminderBeforeDays,
        decimal? toleranceKilometers,
        decimal? toleranceHours,
        int? toleranceDays,
        bool isMandatory,
        bool isActive,
        string? notes)
    {
        try
        {
            var req = new CreateMaintenancePlanRuleRequest(
                maintenanceServiceTypeId,
                scheduleType,
                intervalKilometers,
                intervalMiles,
                intervalEngineHours,
                intervalDays,
                intervalMonths,
                initialDueKilometers,
                initialDueEngineHours,
                initialDueDateUtc,
                reminderBeforeKilometers,
                reminderBeforeEngineHours,
                reminderBeforeDays,
                toleranceKilometers,
                toleranceHours,
                toleranceDays,
                isMandatory,
                isActive,
                notes);

            await _planService.AddRuleAsync(id, req, HttpContext.RequestAborted);
            SuccessMessage = "Maintenance rule added successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteRuleAsync(Guid id, Guid ruleId)
    {
        try
        {
            await _planService.DeleteRuleAsync(id, ruleId, HttpContext.RequestAborted);
            SuccessMessage = "Maintenance rule deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignVehicleAsync(
        Guid id,
        Guid vehicleId,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        decimal? baselineOdometer,
        decimal? baselineEngineHours,
        string? notes)
    {
        try
        {
            var req = new AssignVehiclePlanRequest(
                vehicleId,
                id,
                effectiveFromUtc,
                effectiveToUtc,
                baselineOdometer,
                baselineEngineHours,
                notes);

            await _planService.AssignPlanToVehicleAsync(req, HttpContext.RequestAborted);
            SuccessMessage = "Vehicle assigned to maintenance plan successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveAssignmentAsync(Guid id, Guid assignmentId)
    {
        try
        {
            await _planService.RemoveVehicleAssignmentAsync(assignmentId, HttpContext.RequestAborted);
            SuccessMessage = "Vehicle assignment removed successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}

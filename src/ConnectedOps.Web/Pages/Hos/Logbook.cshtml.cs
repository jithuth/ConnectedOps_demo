using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Hos;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Hos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Hos;

[Authorize]
public class LogbookModel : PageModel
{
    private readonly IHosService _hosService;
    private readonly IDriverService _driverService;
    private readonly IVehicleService _vehicleService;

    public LogbookModel(
        IHosService hosService,
        IDriverService driverService,
        IVehicleService vehicleService)
    {
        _hosService = hosService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public HosRuleConfigurationDto Policy { get; private set; } = null!;
    public HosDriverClocksDto? Clocks { get; private set; }
    public PagedResult<HosLogEntryDto>? Logs { get; private set; }
    public PagedResult<HosViolationDto>? Violations { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DriverId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int LogPage { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var driversPaged = await _driverService.GetDriversPagedAsync(
            new DriverQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Drivers = driversPaged.Items;

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(
            new VehicleQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Vehicles = vehiclesPaged.Items;

        Policy = await _hosService.GetPolicyAsync(cancellationToken);

        if (!DriverId.HasValue && Drivers.Count > 0)
        {
            DriverId = Drivers.First().Id;
        }

        if (DriverId.HasValue)
        {
            Clocks = await _hosService.GetDriverClocksAsync(DriverId.Value, cancellationToken);

            Logs = await _hosService.GetLogsPagedAsync(
                new HosLogFilterRequest(DriverId: DriverId.Value, PageNumber: LogPage, PageSize: 15),
                cancellationToken);

            Violations = await _hosService.GetViolationsPagedAsync(
                new HosViolationFilterRequest(DriverId: DriverId.Value, PageNumber: 1, PageSize: 10),
                cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostChangeDutyStatusAsync(
        Guid targetDriverId,
        DutyStatus newStatus,
        Guid? vehicleId,
        string? locationName,
        decimal? odometer,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new ChangeDutyStatusRequest(
                Status: newStatus,
                VehicleId: vehicleId,
                Odometer: odometer,
                LocationName: locationName,
                Notes: notes);

            await _hosService.ChangeDutyStatusAsync(targetDriverId, req, cancellationToken);
            SuccessMessage = $"Duty status updated to {newStatus} successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { DriverId = targetDriverId });
    }

    public async Task<IActionResult> OnPostUpdatePolicyAsync(
        HosPresetType presetType,
        double maxDrivingHoursPerShift,
        double maxShiftDutyHours,
        double driveHoursBeforeMandatoryBreak,
        int mandatoryBreakMinutes,
        double minConsecutiveOffDutyHours,
        int cycleDays,
        double cycleMaxDutyHours,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new UpdateHosPolicyRequest(
                PresetType: presetType,
                MaxDrivingHoursPerShift: maxDrivingHoursPerShift,
                MaxShiftDutyHours: maxShiftDutyHours,
                DriveHoursBeforeMandatoryBreak: driveHoursBeforeMandatoryBreak,
                MandatoryBreakMinutes: mandatoryBreakMinutes,
                MinConsecutiveOffDutyHours: minConsecutiveOffDutyHours,
                CycleDays: cycleDays,
                CycleMaxDutyHours: cycleMaxDutyHours);

            await _hosService.UpdatePolicyAsync(req, cancellationToken);
            SuccessMessage = $"HOS Policy '{presetType}' updated successfully and applied to active fleet calculations.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { DriverId });
    }

    public async Task<IActionResult> OnPostAcknowledgeViolationAsync(
        Guid violationId,
        Guid targetDriverId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _hosService.AcknowledgeViolationAsync(violationId, cancellationToken);
            SuccessMessage = "Violation acknowledged by safety officer.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { DriverId = targetDriverId });
    }
}

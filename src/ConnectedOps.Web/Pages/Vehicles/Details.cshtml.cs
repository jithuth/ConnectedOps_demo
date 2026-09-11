using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriveType = ConnectedOps.Domain.Vehicles.DriveType;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class DetailsModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IVehicleDocumentService _documentService;
    private readonly IMaintenanceRecordService _maintenanceRecordService;
    private readonly IMaintenanceScheduleService _maintenanceScheduleService;
    private readonly IMaintenancePlanService _maintenancePlanService;
    private readonly IFuelTransactionService _fuelTransactionService;
    private readonly IFuelAnalyticsService _fuelAnalyticsService;

    public DetailsModel(
        IVehicleService vehicleService,
        IVehicleOdometerService odometerService,
        IVehicleDocumentService documentService,
        IMaintenanceRecordService maintenanceRecordService,
        IMaintenanceScheduleService maintenanceScheduleService,
        IMaintenancePlanService maintenancePlanService,
        IFuelTransactionService fuelTransactionService,
        IFuelAnalyticsService fuelAnalyticsService)
    {
        _vehicleService = vehicleService;
        _odometerService = odometerService;
        _documentService = documentService;
        _maintenanceRecordService = maintenanceRecordService;
        _maintenanceScheduleService = maintenanceScheduleService;
        _maintenancePlanService = maintenancePlanService;
        _fuelTransactionService = fuelTransactionService;
        _fuelAnalyticsService = fuelAnalyticsService;
    }

    public Guid VehicleId { get; private set; }
    public VehicleDetailDto Vehicle { get; private set; } = null!;
    public IReadOnlyCollection<VehicleMaintenanceRecordListItemDto> MaintenanceRecords { get; private set; } = [];
    public IReadOnlyCollection<VehicleMaintenanceDueDto> DueMaintenance { get; private set; } = [];
    public IReadOnlyCollection<VehicleMaintenancePlanAssignmentDto> AssignedPlans { get; private set; } = [];
    public PagedResult<FuelTransactionListItemDto>? FuelTransactions { get; private set; }
    public VehicleFuelSummaryDto? FuelSummary { get; private set; }

    [BindProperty]
    public UpsertVehicleSpecificationRequest SpecInput { get; set; } = new(
        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
        DriveType.FWD, null, null, null);

    [BindProperty]
    public CreateVehicleRegistrationRequest RegInput { get; set; } = new(
        string.Empty, null, null, null, null, null, null);

    [BindProperty]
    public CreateVehicleDocumentRequest DocInput { get; set; } = new(
        VehicleDocumentType.Insurance,
        string.Empty, null, null, null, null,
        "docs/sample.pdf", "sample.pdf", "application/pdf", 1024, null);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        VehicleId = id;
        try
        {
            Vehicle = await _vehicleService.GetVehicleByIdAsync(id, HttpContext.RequestAborted);
            MaintenanceRecords = await _maintenanceRecordService.GetVehicleMaintenanceHistoryAsync(id, cancellationToken: HttpContext.RequestAborted);
            DueMaintenance = await _maintenanceScheduleService.CalculateVehicleMaintenanceAsync(id, HttpContext.RequestAborted);
            AssignedPlans = await _maintenancePlanService.GetVehicleAssignmentsAsync(vehicleId: id, cancellationToken: HttpContext.RequestAborted);
            FuelTransactions = await _fuelTransactionService.GetTransactionsPagedAsync(
                new FuelTransactionQueryParameters { VehicleId = id, PageSize = 20 }, HttpContext.RequestAborted);
            FuelSummary = await _fuelAnalyticsService.GetVehicleFuelSummaryAsync(id, HttpContext.RequestAborted);
            if (Vehicle.Specification != null)
            {
                SpecInput = new UpsertVehicleSpecificationRequest(
                    Vehicle.Specification.EngineCapacityCc,
                    Vehicle.Specification.EnginePowerKw,
                    Vehicle.Specification.CylinderCount,
                    Vehicle.Specification.FuelTankCapacity,
                    Vehicle.Specification.BatteryVoltage,
                    Vehicle.Specification.LengthMm,
                    Vehicle.Specification.WidthMm,
                    Vehicle.Specification.HeightMm,
                    Vehicle.Specification.GrossVehicleWeightKg,
                    Vehicle.Specification.KerbWeightKg,
                    Vehicle.Specification.PayloadCapacityKg,
                    Vehicle.Specification.AxleCount,
                    Vehicle.Specification.WheelCount,
                    Vehicle.Specification.SeatCount,
                    Vehicle.Specification.BodyType,
                    Vehicle.Specification.DriveType,
                    Vehicle.Specification.EmissionStandard,
                    Vehicle.Specification.TyreSizeFront,
                    Vehicle.Specification.TyreSizeRear);
            }
            return Page();
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = $"Vehicle '{id}' was not found.";
            return RedirectToPage("/Vehicles/Index");
        }
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, VehicleStatus newStatus, string? notes)
    {
        try
        {
            await _vehicleService.ChangeStatusAsync(id, new ChangeVehicleStatusRequest(newStatus, notes), HttpContext.RequestAborted);
            TempData["Feedback"] = "Vehicle status updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecordOdometerAsync(
        Guid id,
        decimal reading,
        OdometerUnit unit,
        OdometerSource source,
        string? notes)
    {
        try
        {
            await _odometerService.RecordOdometerAsync(
                id,
                new RecordVehicleOdometerRequest(reading, unit, DateTime.UtcNow, source, notes),
                HttpContext.RequestAborted);

            TempData["Feedback"] = $"Odometer recorded: {reading:N0} {unit}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpsertSpecsAsync(Guid id)
    {
        try
        {
            await _vehicleService.UpsertSpecificationAsync(id, SpecInput, HttpContext.RequestAborted);
            TempData["Feedback"] = "Technical specifications updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddRegistrationAsync(Guid id)
    {
        try
        {
            await _vehicleService.AddRegistrationAsync(id, RegInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Registration '{RegInput.RegistrationNumber}' recorded as current registration.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddDocumentAsync(Guid id)
    {
        try
        {
            await _documentService.AddDocumentAsync(id, DocInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Document '{DocInput.Title}' added successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, Guid documentId)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(id, documentId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Document removed successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddNoteAsync(Guid id, string noteText)
    {
        try
        {
            await _vehicleService.AddNoteAsync(id, new CreateVehicleNoteRequest(noteText), HttpContext.RequestAborted);
            TempData["Feedback"] = "Note added successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}

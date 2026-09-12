using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Safety.Incidents;

[Authorize]
public sealed class DetailsModel : PageModel
{
    private readonly ISafetyIncidentService _incidentService;
    private readonly ISafetyViolationService _violationService;
    private readonly ICorrectiveActionService _actionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly IAssetService _assetService;
    private readonly IEmployeeService _employeeService;

    public DetailsModel(
        ISafetyIncidentService incidentService,
        ISafetyViolationService violationService,
        ICorrectiveActionService actionService,
        IVehicleService vehicleService,
        IDriverService driverService,
        IAssetService assetService,
        IEmployeeService employeeService)
    {
        _incidentService = incidentService;
        _violationService = violationService;
        _actionService = actionService;
        _vehicleService = vehicleService;
        _driverService = driverService;
        _assetService = assetService;
        _employeeService = employeeService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public SafetyIncidentDetailDto Incident { get; private set; } = null!;

    public List<SelectListItem> VehiclesList { get; private set; } = [];
    public List<SelectListItem> DriversList { get; private set; } = [];
    public List<SelectListItem> AssetsList { get; private set; } = [];
    public List<SelectListItem> EmployeesList { get; private set; } = [];

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id == Guid.Empty)
            return RedirectToPage("/Safety/Incidents/Index");

        var incident = await _incidentService.GetByIdAsync(Id, cancellationToken);
        if (incident == null)
            return RedirectToPage("/Safety/Incidents/Index");

        Incident = incident;
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostStartInvestigationAsync(Guid? investigatorEmployeeId, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.StartInvestigationAsync(Id, new StartSafetyInvestigationRequest(investigatorEmployeeId), cancellationToken);
            SuccessMessage = "Safety investigation initiated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostCompleteInvestigationAsync(
        string summary,
        SafetyRootCause rootCause,
        string? rootCauseDescription,
        string? contributingFactors,
        string? recommendation,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.CompleteInvestigationAsync(Id, new CompleteSafetyInvestigationRequest(
                Summary: summary,
                RootCause: rootCause,
                RootCauseDescription: rootCauseDescription,
                ContributingFactors: contributingFactors,
                Recommendation: recommendation), cancellationToken);
            SuccessMessage = "Investigation findings and root cause recorded successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostCloseAsync(string? notes, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.CloseAsync(Id, new CloseSafetyIncidentRequest(notes), cancellationToken);
            SuccessMessage = "Incident closed successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddParticipantAsync(
        SafetyParticipantType participantType,
        string role,
        Guid? driverId,
        Guid? employeeId,
        string? name,
        bool injuryReported,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.AddParticipantAsync(Id, new AddIncidentParticipantRequest(
                ParticipantType: participantType,
                Role: role,
                DriverId: driverId,
                EmployeeId: employeeId,
                Name: name,
                InjuryReported: injuryReported,
                Notes: notes), cancellationToken);
            SuccessMessage = "Participant added to incident.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.RemoveParticipantAsync(Id, participantId, cancellationToken);
            SuccessMessage = "Participant removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddVehicleAsync(
        Guid vehicleId,
        bool damageReported,
        string? damageDescription,
        bool isPrimaryVehicle,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.AddVehicleAsync(Id, new AddIncidentVehicleRequest(
                VehicleId: vehicleId,
                DamageReported: damageReported,
                DamageDescription: damageDescription,
                IsPrimaryVehicle: isPrimaryVehicle), cancellationToken);
            SuccessMessage = "Vehicle added to incident.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveVehicleAsync(Guid incidentVehicleId, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.RemoveVehicleAsync(Id, incidentVehicleId, cancellationToken);
            SuccessMessage = "Vehicle removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddAssetAsync(
        Guid assetId,
        bool damageReported,
        string? damageDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.AddAssetAsync(Id, new AddIncidentAssetRequest(
                AssetId: assetId,
                DamageReported: damageReported,
                DamageDescription: damageDescription), cancellationToken);
            SuccessMessage = "Asset added to incident.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveAssetAsync(Guid incidentAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.RemoveAssetAsync(Id, incidentAssetId, cancellationToken);
            SuccessMessage = "Asset removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddEvidenceAsync(
        SafetyEvidenceType evidenceType,
        string title,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = $"evidence/{Id}/{Guid.NewGuid():N}_{fileName}";
            await _incidentService.AddEvidenceAsync(Id, new AddIncidentEvidenceRequest(
                EvidenceType: evidenceType,
                Title: title,
                FileObjectKey: key,
                FileName: fileName,
                ContentType: string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                FileSizeBytes: fileSizeBytes > 0 ? fileSizeBytes : 1024,
                CapturedAtUtc: DateTime.UtcNow,
                Notes: notes), cancellationToken);
            SuccessMessage = "Evidence logged.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken)
    {
        try
        {
            await _incidentService.RemoveEvidenceAsync(Id, evidenceId, cancellationToken);
            SuccessMessage = "Evidence removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddViolationAsync(
        SafetyViolationType violationType,
        SafetyIncidentSeverity severity,
        DateTime occurredAtUtc,
        string description,
        Guid? driverId,
        Guid? vehicleId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _violationService.CreateAsync(new CreateSafetyViolationRequest(
                ViolationType: violationType,
                Severity: severity,
                Source: SafetyViolationSource.Manual,
                OccurredAtUtc: occurredAtUtc,
                Description: description,
                DriverId: driverId,
                VehicleId: vehicleId,
                SafetyIncidentId: Id), cancellationToken);
            SuccessMessage = "Violation linked to incident.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddActionAsync(
        string title,
        string description,
        CorrectiveActionPriority priority,
        DateTime? dueDateUtc,
        Guid? assignedEmployeeId,
        bool verificationRequired,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.CreateAsync(new CreateCorrectiveActionRequest(
                Title: title,
                Description: description,
                Priority: priority,
                DueDateUtc: dueDateUtc,
                AssignedEmployeeId: assignedEmployeeId,
                SafetyIncidentId: Id,
                VerificationRequired: verificationRequired), cancellationToken);
            SuccessMessage = "Corrective action assigned.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostStartActionAsync(Guid actionId, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.StartAsync(actionId, cancellationToken);
            SuccessMessage = "Action in progress.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostCompleteActionAsync(Guid actionId, string? notes, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.CompleteAsync(actionId, new CompleteCorrectiveActionRequest(notes), cancellationToken);
            SuccessMessage = "Action completed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostVerifyActionAsync(Guid actionId, string? notes, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.VerifyAsync(actionId, new VerifyCorrectiveActionRequest(notes), cancellationToken);
            SuccessMessage = "Action verified.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Incidents/Details", new { id = Id });
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        VehiclesList = vehicles.Items.Select(v => new SelectListItem($"{v.VehicleNumber} - {v.DisplayName}", v.Id.ToString())).ToList();

        var drivers = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        DriversList = drivers.Items.Select(d => new SelectListItem($"{d.DisplayName} ({d.PrimaryLicenseNumber ?? "N/A"})", d.Id.ToString())).ToList();

        var assets = await _assetService.GetPagedAsync(new AssetListFilter(PageNumber: 1, PageSize: 100), cancellationToken);
        AssetsList = assets.Items.Select(a => new SelectListItem($"{a.AssetNumber} - {a.Name}", a.Id.ToString())).ToList();

        var employees = await _employeeService.GetEmployeesAsync(cancellationToken: cancellationToken);
        EmployeesList = employees.Select(e => new SelectListItem($"{e.FullName} ({e.JobTitle ?? "Staff"})", e.Id.ToString())).ToList();
    }
}

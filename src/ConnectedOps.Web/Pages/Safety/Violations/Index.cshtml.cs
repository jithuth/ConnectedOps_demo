using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Safety.Violations;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly ISafetyViolationService _violationService;
    private readonly IDriverService _driverService;
    private readonly IVehicleService _vehicleService;

    public IndexModel(
        ISafetyViolationService violationService,
        IDriverService driverService,
        IVehicleService vehicleService)
    {
        _violationService = violationService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    [BindProperty(SupportsGet = true)]
    public SafetyViolationType? ViolationType { get; set; }

    [BindProperty(SupportsGet = true)]
    public SafetyIncidentSeverity? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public SafetyViolationSource? Source { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsResolved { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<SafetyViolationDto> Violations { get; private set; } = null!;
    public List<SelectListItem> DriversList { get; private set; } = [];
    public List<SelectListItem> VehiclesList { get; private set; } = [];

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        SafetyViolationType violationType,
        SafetyIncidentSeverity severity,
        SafetyViolationSource source,
        DateTime occurredAtUtc,
        string description,
        Guid? driverId,
        Guid? vehicleId,
        string? reference,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Violation description is required.";
            await LoadDataAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _violationService.CreateAsync(new CreateSafetyViolationRequest(
                ViolationType: violationType,
                Severity: severity,
                Source: source,
                OccurredAtUtc: occurredAtUtc,
                Description: description.Trim(),
                DriverId: driverId,
                VehicleId: vehicleId,
                Reference: reference), cancellationToken);

            SuccessMessage = "Safety infraction logged successfully.";
            return RedirectToPage("/Safety/Violations/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid id, string? resolutionNotes, CancellationToken cancellationToken)
    {
        try
        {
            await _violationService.ResolveAsync(id, new ResolveSafetyViolationRequest(resolutionNotes), cancellationToken);
            SuccessMessage = "Safety violation marked as resolved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/Violations/Index");
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        var filter = new SafetyViolationFilterRequest(
            ViolationType: ViolationType,
            Severity: Severity,
            Source: Source,
            IsResolved: IsResolved,
            SearchTerm: SearchTerm,
            Page: PageNumber,
            PageSize: 20);

        Violations = await _violationService.GetPagedAsync(filter, cancellationToken);

        var drivers = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        DriversList = drivers.Items.Select(d => new SelectListItem($"{d.DisplayName} ({d.PrimaryLicenseNumber ?? "N/A"})", d.Id.ToString())).ToList();

        var vehicles = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        VehiclesList = vehicles.Items.Select(v => new SelectListItem($"{v.VehicleNumber} - {v.DisplayName}", v.Id.ToString())).ToList();
    }
}

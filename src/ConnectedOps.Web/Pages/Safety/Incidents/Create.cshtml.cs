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
public sealed class CreateModel : PageModel
{
    private readonly ISafetyIncidentService _incidentService;
    private readonly IBranchService _branchService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public CreateModel(
        ISafetyIncidentService incidentService,
        IBranchService branchService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _incidentService = incidentService;
        _branchService = branchService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    [BindProperty]
    public IncidentInput Form { get; set; } = new();

    public List<SelectListItem> Branches { get; private set; } = [];
    public List<SelectListItem> Vehicles { get; private set; } = [];
    public List<SelectListItem> Drivers { get; private set; } = [];

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDropdownsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Form.Title) || string.IsNullOrWhiteSpace(Form.Description))
        {
            ErrorMessage = "Incident Title and Detailed Description are required.";
            await LoadDropdownsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var req = new CreateSafetyIncidentRequest(
                IncidentType: Form.IncidentType,
                Severity: Form.Severity,
                OccurredAtUtc: Form.OccurredAtUtc,
                Title: Form.Title.Trim(),
                Description: Form.Description.Trim(),
                BranchId: Form.BranchId,
                Latitude: Form.Latitude,
                Longitude: Form.Longitude,
                ImmediateActionTaken: Form.ImmediateActionTaken,
                InvestigationRequired: Form.InvestigationRequired,
                PrimaryVehicleId: Form.PrimaryVehicleId,
                PrimaryDriverId: Form.PrimaryDriverId);

            var created = await _incidentService.CreateAsync(req, cancellationToken);
            return RedirectToPage("/Safety/Incidents/Details", new { id = created.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDropdownsAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadDropdownsAsync(CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetBranchesAsync(cancellationToken);
        Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == Form.BranchId)).ToList();

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        Vehicles = vehiclesPaged.Items.Select(v => new SelectListItem($"{v.VehicleNumber} - {v.DisplayName}", v.Id.ToString(), v.Id == Form.PrimaryVehicleId)).ToList();

        var driversPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageNumber = 1, PageSize = 100 }, cancellationToken);
        Drivers = driversPaged.Items.Select(d => new SelectListItem($"{d.DisplayName} ({d.PrimaryLicenseNumber ?? "N/A"})", d.Id.ToString(), d.Id == Form.PrimaryDriverId)).ToList();
    }

    public sealed class IncidentInput
    {
        public SafetyIncidentType IncidentType { get; set; } = SafetyIncidentType.VehicleAccident;
        public SafetyIncidentSeverity Severity { get; set; } = SafetyIncidentSeverity.Moderate;
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ImmediateActionTaken { get; set; }
        public bool InvestigationRequired { get; set; } = true;
        public Guid? PrimaryVehicleId { get; set; }
        public Guid? PrimaryDriverId { get; set; }
    }
}

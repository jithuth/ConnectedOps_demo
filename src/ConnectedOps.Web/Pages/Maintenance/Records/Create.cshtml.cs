using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Records;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMaintenanceRecordService _recordService;
    private readonly IVehicleService _vehicleService;
    private readonly IMaintenanceServiceTypeService _serviceTypeService;
    private readonly IMaintenanceProviderService _providerService;
    private readonly IMaintenancePlanService _planService;

    public CreateModel(
        IMaintenanceRecordService recordService,
        IVehicleService vehicleService,
        IMaintenanceServiceTypeService serviceTypeService,
        IMaintenanceProviderService providerService,
        IMaintenancePlanService planService)
    {
        _recordService = recordService;
        _vehicleService = vehicleService;
        _serviceTypeService = serviceTypeService;
        _providerService = providerService;
        _planService = planService;
    }

    [BindProperty]
    public CreateMaintenanceRecordRequest Input { get; set; } = new(
        Guid.Empty,
        Guid.Empty,
        DateTime.UtcNow);

    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<MaintenanceServiceTypeDto> ServiceTypes { get; private set; } = [];
    public IReadOnlyCollection<MaintenanceProviderDto> Providers { get; private set; } = [];
    public IReadOnlyCollection<MaintenancePlanListItemDto> Plans { get; private set; } = [];

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? serviceTypeId,
        [FromQuery] Guid? planId,
        [FromQuery] Guid? ruleId)
    {
        await LoadCatalogsAsync();

        if (vehicleId.HasValue)
        {
            var vehicle = Vehicles.FirstOrDefault(v => v.Id == vehicleId.Value);
            Input = Input with
            {
                VehicleId = vehicleId.Value,
                OdometerReading = vehicle?.CurrentOdometer,
                OdometerUnit = vehicle?.OdometerUnit ?? OdometerUnit.Kilometers,
                MaintenanceServiceTypeId = serviceTypeId ?? Guid.Empty,
                MaintenancePlanId = planId,
                MaintenancePlanRuleId = ruleId
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCatalogsAsync();
            return Page();
        }

        try
        {
            var record = await _recordService.CreateAsync(Input);
            return RedirectToPage("/Maintenance/Records/Details", new { id = record.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadCatalogsAsync();
            return Page();
        }
    }

    private async Task LoadCatalogsAsync()
    {
        var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 1000, IsActive = true });
        Vehicles = vResult.Items;
        ServiceTypes = await _serviceTypeService.GetAllAsync(activeOnly: true);
        Providers = await _providerService.GetAllAsync(activeOnly: true);
        Plans = await _planService.GetAllAsync(activeOnly: true);
    }
}

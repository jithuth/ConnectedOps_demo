using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Records;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMaintenanceRecordService _recordService;
    private readonly IMaintenanceServiceTypeService _serviceTypeService;
    private readonly IMaintenanceProviderService _providerService;

    public IndexModel(
        IMaintenanceRecordService recordService,
        IMaintenanceServiceTypeService serviceTypeService,
        IMaintenanceProviderService providerService)
    {
        _recordService = recordService;
        _serviceTypeService = serviceTypeService;
        _providerService = providerService;
    }

    [BindProperty(SupportsGet = true)]
    public MaintenanceRecordQueryParameters Query { get; set; } = new();

    public PagedResult<VehicleMaintenanceRecordListItemDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<MaintenanceServiceTypeDto> ServiceTypes { get; private set; } = [];
    public IReadOnlyCollection<MaintenanceProviderDto> Providers { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Result = await _recordService.GetRecordsAsync(Query);
        ServiceTypes = await _serviceTypeService.GetAllAsync();
        Providers = await _providerService.GetAllAsync();
    }
}

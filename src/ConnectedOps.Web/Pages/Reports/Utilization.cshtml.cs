using ConnectedOps.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Reports;

[Authorize]
public sealed class UtilizationModel : PageModel
{
    private readonly IExecutiveReportService _reportService;

    public UtilizationModel(IExecutiveReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleCategoryId { get; set; }

    public FleetUtilizationReportDto Report { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = StartDate,
            ToDateUtc = EndDate,
            VehicleCategoryId = VehicleCategoryId
        };

        Report = await _reportService.GetFleetUtilizationReportAsync(filter, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = StartDate,
            ToDateUtc = EndDate,
            VehicleCategoryId = VehicleCategoryId
        };

        var export = await _reportService.ExportReportCsvAsync("utilization", filter, HttpContext.RequestAborted);
        return File(export.FileBytes, export.ContentType, export.FileName);
    }
}

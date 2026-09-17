using ConnectedOps.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Reports;

[Authorize]
public sealed class TcoModel : PageModel
{
    private readonly IExecutiveReportService _reportService;

    public TcoModel(IExecutiveReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleCategoryId { get; set; }

    public FleetTcoReportDto Report { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = StartDate,
            ToDateUtc = EndDate,
            VehicleCategoryId = VehicleCategoryId
        };

        Report = await _reportService.GetFleetTcoReportAsync(filter, HttpContext.RequestAborted);
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

        var export = await _reportService.ExportReportCsvAsync("tco", filter, HttpContext.RequestAborted);
        return File(export.FileBytes, export.ContentType, export.FileName);
    }
}

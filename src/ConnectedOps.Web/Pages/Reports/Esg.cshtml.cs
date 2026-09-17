using ConnectedOps.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Reports;

[Authorize]
public sealed class EsgModel : PageModel
{
    private readonly IExecutiveReportService _reportService;

    public EsgModel(IExecutiveReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public EsgCarbonReportDto Report { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = StartDate,
            ToDateUtc = EndDate
        };

        Report = await _reportService.GetEsgCarbonReportAsync(filter, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = StartDate,
            ToDateUtc = EndDate
        };

        var export = await _reportService.ExportReportCsvAsync("esg", filter, HttpContext.RequestAborted);
        return File(export.FileBytes, export.ContentType, export.FileName);
    }
}

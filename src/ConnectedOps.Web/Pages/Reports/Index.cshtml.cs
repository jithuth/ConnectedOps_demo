using ConnectedOps.Application.Reports;
using ConnectedOps.Domain.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Reports;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IExecutiveReportService _reportService;

    public IndexModel(IExecutiveReportService reportService)
    {
        _reportService = reportService;
    }

    public IReadOnlyList<ReportDefinition> Catalog { get; private set; } = Array.Empty<ReportDefinition>();

    public async Task<IActionResult> OnGetAsync()
    {
        Catalog = await _reportService.GetAvailableReportsAsync(HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(string code, DateTime? startDate, DateTime? endDate)
    {
        var filter = new ReportFilterParameters
        {
            FromDateUtc = startDate,
            ToDateUtc = endDate
        };

        var export = await _reportService.ExportReportCsvAsync(code, filter, HttpContext.RequestAborted);
        return File(export.FileBytes, export.ContentType, export.FileName);
    }
}

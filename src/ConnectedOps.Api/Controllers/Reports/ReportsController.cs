using ConnectedOps.Application.Reports;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Reports;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IExecutiveReportService _reportService;

    public ReportsController(IExecutiveReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("executive-summary")]
    [RequirePermission(PermissionKeys.Reports.ViewDashboard)]
    public async Task<IActionResult> GetExecutiveSummary(
        [FromQuery] ReportFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetExecutiveSummaryAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tco")]
    [RequirePermission(PermissionKeys.Reports.ViewTco)]
    public async Task<IActionResult> GetFleetTco(
        [FromQuery] ReportFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetFleetTcoReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("utilization")]
    [RequirePermission(PermissionKeys.Reports.ViewUtilization)]
    public async Task<IActionResult> GetFleetUtilization(
        [FromQuery] ReportFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetFleetUtilizationReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("esg")]
    [RequirePermission(PermissionKeys.Reports.ViewEsg)]
    public async Task<IActionResult> GetEsgCarbon(
        [FromQuery] ReportFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetEsgCarbonReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("catalog")]
    [RequirePermission(PermissionKeys.Reports.ViewDashboard)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAvailableReportsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("export/{reportType}")]
    [RequirePermission(PermissionKeys.Reports.Export)]
    public async Task<IActionResult> ExportReport(
        string reportType,
        [FromQuery] ReportFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.ExportReportCsvAsync(reportType, filter, cancellationToken);
        return File(result.FileBytes, result.ContentType, result.FileName);
    }
}

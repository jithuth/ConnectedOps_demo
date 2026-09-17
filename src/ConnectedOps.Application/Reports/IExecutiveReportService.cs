using ConnectedOps.Domain.Reports;

namespace ConnectedOps.Application.Reports;

public interface IExecutiveReportService
{
    Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default);

    Task<FleetTcoReportDto> GetFleetTcoReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default);

    Task<FleetUtilizationReportDto> GetFleetUtilizationReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default);

    Task<EsgCarbonReportDto> GetEsgCarbonReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default);

    Task<List<ReportDefinition>> GetAvailableReportsAsync(
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportReportCsvAsync(
        string reportType,
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default);
}

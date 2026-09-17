using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Reports;

public enum ReportExportFormat
{
    Json = 1,
    Csv = 2,
    Excel = 3,
    Pdf = 4
}

public sealed class ReportExecutionLog : BaseEntity
{
    private ReportExecutionLog()
    {
    }

    public ReportExecutionLog(
        Guid tenantId,
        string reportCode,
        string reportName,
        ReportExportFormat format,
        string? filterCriteriaJson,
        long recordCount,
        long executionDurationMs,
        Guid? generatedByUserId,
        string? generatedByUserName)
    {
        if (string.IsNullOrWhiteSpace(reportCode))
            throw new ArgumentException("ReportCode is required.", nameof(reportCode));

        TenantId = tenantId;
        ReportCode = reportCode.Trim().ToUpperInvariant();
        ReportName = reportName.Trim();
        Format = format;
        FilterCriteriaJson = filterCriteriaJson;
        RecordCount = recordCount;
        ExecutionDurationMs = executionDurationMs;
        GeneratedByUserId = generatedByUserId;
        GeneratedByUserName = generatedByUserName;
    }

    public Guid TenantId { get; private set; }
    public string ReportCode { get; private set; } = string.Empty;
    public string ReportName { get; private set; } = string.Empty;
    public ReportExportFormat Format { get; private set; }
    public string? FilterCriteriaJson { get; private set; }
    public long RecordCount { get; private set; }
    public long ExecutionDurationMs { get; private set; }
    public Guid? GeneratedByUserId { get; private set; }
    public string? GeneratedByUserName { get; private set; }
}

using ConnectedOps.Domain.FleetOperations;

namespace ConnectedOps.Application.FleetOperations;

public sealed record FleetOperationalExceptionDto(
    Guid Id,
    Guid TenantId,
    Guid? VehicleId,
    string? VehicleNumber,
    string? VehicleDisplayName,
    Guid? DriverId,
    string? DriverNumber,
    string? DriverDisplayName,
    Guid? UsageSessionId,
    OperationalExceptionType ExceptionType,
    string ExceptionTypeName,
    OperationalExceptionSeverity Severity,
    string SeverityName,
    string Description,
    DateTime OccurredAtUtc,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    string? ResolvedByUserName,
    string? ResolutionNotes,
    OperationalExceptionStatus Status,
    string StatusName,
    DateTime CreatedAtUtc);

public sealed record CreateOperationalExceptionRequest(
    OperationalExceptionType ExceptionType,
    OperationalExceptionSeverity Severity,
    string Description,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? UsageSessionId = null,
    DateTime? OccurredAtUtc = null);

public sealed record ResolveOperationalExceptionRequest(
    string ResolutionNotes);

public sealed record DismissOperationalExceptionRequest(
    string? DismissalNotes = null);

public sealed class OperationalExceptionQueryParameters
{
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public OperationalExceptionType? Type { get; set; }
    public OperationalExceptionSeverity? Severity { get; set; }
    public OperationalExceptionStatus? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

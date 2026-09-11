using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelAnomalyDto(
    Guid Id,
    Guid FuelTransactionId,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    FuelAnomalyType AnomalyType,
    string AnomalyTypeName,
    FuelAnomalySeverity Severity,
    string SeverityName,
    string Description,
    DateTime DetectedAtUtc,
    FuelAnomalyStatus Status,
    string StatusName,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    string? ResolvedByUserName,
    string? ResolutionNotes);

public sealed record ResolveFuelAnomalyRequest(
    string? ResolutionNotes = null);

public sealed record DismissFuelAnomalyRequest(
    string? DismissalReason = null);

public sealed record FuelAnomalyQueryParameters
{
    public FuelAnomalyStatus? Status { get; init; }
    public FuelAnomalyType? AnomalyType { get; init; }
    public FuelAnomalySeverity? Severity { get; init; }
    public Guid? VehicleId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

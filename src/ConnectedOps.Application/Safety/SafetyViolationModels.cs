using ConnectedOps.Domain.Safety;

namespace ConnectedOps.Application.Safety;

public sealed record SafetyViolationDto(
    Guid Id,
    Guid TenantId,
    SafetyViolationType ViolationType,
    SafetyIncidentSeverity Severity,
    SafetyViolationSource Source,
    DateTime OccurredAtUtc,
    string Description,
    Guid? DriverId,
    string? DriverName,
    Guid? EmployeeId,
    string? EmployeeName,
    Guid? VehicleId,
    string? VehiclePlate,
    Guid? SafetyIncidentId,
    string? IncidentNumber,
    string? Reference,
    bool IsResolved,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    string? ResolvedByUserName,
    string? ResolutionNotes,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateSafetyViolationRequest(
    SafetyViolationType ViolationType,
    SafetyIncidentSeverity Severity,
    SafetyViolationSource Source,
    DateTime OccurredAtUtc,
    string Description,
    Guid? DriverId = null,
    Guid? EmployeeId = null,
    Guid? VehicleId = null,
    Guid? SafetyIncidentId = null,
    string? Reference = null,
    string? Notes = null);

public sealed record UpdateSafetyViolationRequest(
    SafetyViolationType ViolationType,
    SafetyIncidentSeverity Severity,
    SafetyViolationSource Source,
    DateTime OccurredAtUtc,
    string Description,
    Guid? DriverId = null,
    Guid? EmployeeId = null,
    Guid? VehicleId = null,
    Guid? SafetyIncidentId = null,
    string? Reference = null,
    string? Notes = null);

public sealed record ResolveSafetyViolationRequest(
    string? ResolutionNotes = null);

public sealed record SafetyViolationFilterRequest(
    SafetyViolationType? ViolationType = null,
    SafetyIncidentSeverity? Severity = null,
    SafetyViolationSource? Source = null,
    bool? IsResolved = null,
    Guid? DriverId = null,
    Guid? VehicleId = null,
    Guid? EmployeeId = null,
    Guid? SafetyIncidentId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50);

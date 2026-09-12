using ConnectedOps.Domain.Safety;

namespace ConnectedOps.Application.Safety;

public sealed record SafetyIncidentDto(
    Guid Id,
    Guid TenantId,
    string IncidentNumber,
    SafetyIncidentType IncidentType,
    SafetyIncidentSeverity Severity,
    SafetyIncidentStatus Status,
    DateTime OccurredAtUtc,
    DateTime ReportedAtUtc,
    Guid? BranchId,
    string? BranchName,
    Guid? LocationId,
    string? LocationName,
    double? Latitude,
    double? Longitude,
    string Title,
    string Description,
    string? ImmediateActionTaken,
    Guid? ReportedByUserId,
    string? ReportedByUserName,
    bool InvestigationRequired,
    SafetyInvestigationStatus InvestigationStatus,
    int ParticipantCount,
    int VehicleCount,
    int AssetCount,
    int EvidenceCount,
    int CorrectiveActionCount,
    int OpenCorrectiveActionCount,
    DateTime? ClosedAtUtc,
    Guid? ClosedByUserId,
    DateTime CreatedAtUtc);

public sealed record SafetyIncidentDetailDto(
    Guid Id,
    Guid TenantId,
    string IncidentNumber,
    SafetyIncidentType IncidentType,
    SafetyIncidentSeverity Severity,
    SafetyIncidentStatus Status,
    DateTime OccurredAtUtc,
    DateTime ReportedAtUtc,
    Guid? BranchId,
    string? BranchName,
    Guid? LocationId,
    string? LocationName,
    double? Latitude,
    double? Longitude,
    string Title,
    string Description,
    string? ImmediateActionTaken,
    Guid? ReportedByUserId,
    string? ReportedByUserName,
    bool InvestigationRequired,
    DateTime? ClosedAtUtc,
    Guid? ClosedByUserId,
    DateTime CreatedAtUtc,
    SafetyIncidentInvestigationDto? Investigation,
    IReadOnlyList<SafetyIncidentParticipantDto> Participants,
    IReadOnlyList<SafetyIncidentVehicleDto> Vehicles,
    IReadOnlyList<SafetyIncidentAssetDto> Assets,
    IReadOnlyList<SafetyIncidentEvidenceDto> Evidence,
    IReadOnlyList<CorrectiveActionDto> CorrectiveActions,
    IReadOnlyList<SafetyViolationDto> Violations);

public sealed record SafetyIncidentParticipantDto(
    Guid Id,
    Guid TenantId,
    Guid SafetyIncidentId,
    SafetyParticipantType ParticipantType,
    string Role,
    Guid? DriverId,
    string? DriverName,
    Guid? EmployeeId,
    string? EmployeeName,
    string? Name,
    bool InjuryReported,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record SafetyIncidentVehicleDto(
    Guid Id,
    Guid TenantId,
    Guid SafetyIncidentId,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleMakeModel,
    bool DamageReported,
    string? DamageDescription,
    bool IsPrimaryVehicle,
    DateTime CreatedAtUtc);

public sealed record SafetyIncidentAssetDto(
    Guid Id,
    Guid TenantId,
    Guid SafetyIncidentId,
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    bool DamageReported,
    string? DamageDescription,
    DateTime CreatedAtUtc);

public sealed record SafetyIncidentEvidenceDto(
    Guid Id,
    Guid TenantId,
    Guid SafetyIncidentId,
    SafetyEvidenceType EvidenceType,
    string Title,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? CapturedAtUtc,
    DateTime UploadedAtUtc,
    Guid? UploadedByUserId,
    string? Notes);

public sealed record SafetyIncidentInvestigationDto(
    Guid Id,
    Guid TenantId,
    Guid SafetyIncidentId,
    Guid? InvestigatorEmployeeId,
    string? InvestigatorEmployeeName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Summary,
    SafetyRootCause RootCause,
    string? RootCauseDescription,
    string? ContributingFactors,
    string? Recommendation,
    SafetyInvestigationStatus Status,
    DateTime CreatedAtUtc);

public sealed record CreateSafetyIncidentRequest(
    SafetyIncidentType IncidentType,
    SafetyIncidentSeverity Severity,
    DateTime OccurredAtUtc,
    string Title,
    string Description,
    Guid? BranchId = null,
    Guid? LocationId = null,
    double? Latitude = null,
    double? Longitude = null,
    string? ImmediateActionTaken = null,
    bool InvestigationRequired = false,
    Guid? PrimaryVehicleId = null,
    Guid? PrimaryDriverId = null,
    string? InitialEvidenceTitle = null,
    string? InitialEvidenceKey = null,
    string? InitialEvidenceFileName = null);

public sealed record UpdateSafetyIncidentRequest(
    SafetyIncidentType IncidentType,
    SafetyIncidentSeverity Severity,
    DateTime OccurredAtUtc,
    string Title,
    string Description,
    Guid? BranchId = null,
    Guid? LocationId = null,
    double? Latitude = null,
    double? Longitude = null,
    string? ImmediateActionTaken = null,
    bool InvestigationRequired = false);

public sealed record StartSafetyInvestigationRequest(
    Guid? InvestigatorEmployeeId = null);

public sealed record CompleteSafetyInvestigationRequest(
    string Summary,
    SafetyRootCause RootCause,
    string? RootCauseDescription = null,
    string? ContributingFactors = null,
    string? Recommendation = null);

public sealed record CloseSafetyIncidentRequest(
    string? Notes = null);

public sealed record AddIncidentParticipantRequest(
    SafetyParticipantType ParticipantType,
    string Role,
    Guid? DriverId = null,
    Guid? EmployeeId = null,
    string? Name = null,
    bool InjuryReported = false,
    string? Notes = null);

public sealed record AddIncidentVehicleRequest(
    Guid VehicleId,
    bool DamageReported = false,
    string? DamageDescription = null,
    bool IsPrimaryVehicle = false);

public sealed record AddIncidentAssetRequest(
    Guid AssetId,
    bool DamageReported = false,
    string? DamageDescription = null);

public sealed record AddIncidentEvidenceRequest(
    SafetyEvidenceType EvidenceType,
    string Title,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? CapturedAtUtc = null,
    string? Notes = null);

public sealed record SafetyIncidentFilterRequest(
    SafetyIncidentStatus? Status = null,
    SafetyIncidentSeverity? Severity = null,
    SafetyIncidentType? IncidentType = null,
    Guid? BranchId = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? AssetId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50);

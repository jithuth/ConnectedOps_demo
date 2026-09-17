using ConnectedOps.Domain.TollsAndFines;

namespace ConnectedOps.Application.TollsAndFines;

public sealed record TollFilterRequest(
    TollSystemType? TollSystem = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ViolationFilterRequest(
    ViolationLiabilityStatus? LiabilityStatus = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record TollTransactionDto(
    Guid Id,
    TollSystemType TollSystem,
    string TollSystemName,
    string TollGateName,
    string TollGateCode,
    decimal Amount,
    DateTime TransactionTimeUtc,
    Guid VehicleId,
    string VehiclePlateNumber,
    string? TagNumber,
    Guid? MatchedDriverId,
    string? MatchedDriverName,
    Guid? MatchedUsageSessionId);

public sealed record CreateTollTransactionRequest(
    TollSystemType TollSystem,
    string TollGateName,
    string TollGateCode,
    decimal Amount,
    DateTime TransactionTimeUtc,
    Guid VehicleId,
    string? TagNumber = null);

public sealed record TrafficViolationDto(
    Guid Id,
    string TicketNumber,
    string AuthorityName,
    string ViolationCode,
    string Description,
    decimal FineAmount,
    int BlackPoints,
    DateTime ViolationTimeUtc,
    string? Location,
    Guid VehicleId,
    string VehiclePlateNumber,
    Guid? MatchedDriverId,
    string? MatchedDriverName,
    Guid? MatchedUsageSessionId,
    ViolationLiabilityStatus LiabilityStatus,
    string LiabilityStatusName,
    string? DisputeReason,
    string? ResolutionNotes,
    DateTime? SettledAtUtc);

public sealed record CreateTrafficViolationRequest(
    string TicketNumber,
    string AuthorityName,
    string ViolationCode,
    string Description,
    decimal FineAmount,
    int BlackPoints,
    DateTime ViolationTimeUtc,
    Guid VehicleId,
    string? Location = null);

public sealed record AssignViolationLiabilityRequest(
    Guid DriverId,
    Guid? UsageSessionId = null);

public sealed record DisputeViolationRequest(
    string DisputeReason);

public sealed record SettleViolationRequest(
    ViolationLiabilityStatus Status,
    string? Notes = null);

public sealed record TollsAndFinesDashboardDto(
    decimal TotalTollsAmountThisMonth,
    int TotalTollsCountThisMonth,
    decimal TotalFinesAmountPending,
    int PendingFinesCount,
    int DisputedFinesCount,
    List<TollTransactionDto> RecentTolls,
    List<TrafficViolationDto> RecentViolations);

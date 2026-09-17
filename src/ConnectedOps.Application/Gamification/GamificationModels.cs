using ConnectedOps.Domain.Gamification;

namespace ConnectedOps.Application.Gamification;

public sealed record DriverScorecardDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    int PeriodMonth,
    int PeriodYear,
    double OverallScore,
    double SafetyScore,
    double EcoScore,
    double ComplianceScore,
    double DispatchScore,
    int HarshBrakingCount,
    int HarshAccelerationCount,
    int SpeedingEventsCount,
    double IdleHours,
    DriverTier Tier,
    string TierName,
    int RankInFleet);

public sealed record DriverBadgeDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    BadgeType BadgeType,
    string BadgeTypeName,
    string Title,
    string Description,
    int Points,
    DateTime EarnedAtUtc);

public sealed record AwardBadgeRequest(
    Guid DriverId,
    BadgeType BadgeType,
    string Title,
    string Description,
    int Points = 100);

public sealed record FleetLeaderboardDto(
    int PeriodMonth,
    int PeriodYear,
    int TotalRankedDrivers,
    double FleetAverageScore,
    List<DriverScorecardDto> TopThreePodium,
    List<DriverScorecardDto> Rankings);

public sealed record ScorecardFilterRequest(
    int? Month = null,
    int? Year = null,
    DriverTier? Tier = null,
    int PageNumber = 1,
    int PageSize = 20);

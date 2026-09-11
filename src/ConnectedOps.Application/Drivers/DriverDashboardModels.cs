using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Drivers;

public sealed record DriverDashboardSummaryDto(
    int TotalDrivers,
    int ActiveDrivers,
    int AvailableDrivers,
    int AssignedDrivers,
    int OnLeaveDrivers,
    int SuspendedDrivers,
    int InactiveDrivers,
    int ExpiredLicenses,
    int LicensesExpiringSoon,
    int ExpiredDocuments,
    int DocumentsExpiringSoon,
    int ExpiredCertifications,
    int CertificationsExpiringSoon,
    IReadOnlyCollection<DriverBranchMetricDto> DriversByBranch,
    IReadOnlyCollection<DriverTypeMetricDto> DriversByType,
    IReadOnlyCollection<DriverStatusMetricDto> DriversByStatus,
    IReadOnlyCollection<DriverListItemDto> RecentDrivers,
    IReadOnlyCollection<DriverExpiringItemDto> UpcomingExpiries);

public sealed record DriverBranchMetricDto(
    Guid? BranchId,
    string BranchName,
    int DriverCount);

public sealed record DriverTypeMetricDto(
    DriverType Type,
    string TypeName,
    int DriverCount);

public sealed record DriverStatusMetricDto(
    DriverStatus Status,
    string StatusName,
    int DriverCount);

public sealed record DriverExpiringItemDto(
    Guid DriverId,
    string DriverNumber,
    string DriverName,
    string ItemType,
    string ItemTitle,
    DateOnly ExpiryDate,
    int DaysRemaining,
    bool IsExpired);

using ConnectedOps.Domain.Hos;

namespace ConnectedOps.Application.Hos;

public sealed record HosRuleConfigurationDto(
    Guid Id,
    HosPresetType PresetType,
    string PresetName,
    double MaxDrivingHoursPerShift,
    double MaxShiftDutyHours,
    double DriveHoursBeforeMandatoryBreak,
    int MandatoryBreakMinutes,
    double MinConsecutiveOffDutyHours,
    int CycleDays,
    double CycleMaxDutyHours);

public sealed record UpdateHosPolicyRequest(
    HosPresetType PresetType,
    double? MaxDrivingHoursPerShift = null,
    double? MaxShiftDutyHours = null,
    double? DriveHoursBeforeMandatoryBreak = null,
    int? MandatoryBreakMinutes = null,
    double? MinConsecutiveOffDutyHours = null,
    int? CycleDays = null,
    double? CycleMaxDutyHours = null);

public sealed record HosLogEntryDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    DutyStatus Status,
    string StatusName,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    double DurationHours,
    Guid? VehicleId,
    string? VehiclePlateNumber,
    decimal? StartOdometer,
    decimal? EndOdometer,
    string? LocationName,
    double? Latitude,
    double? Longitude,
    string? Notes,
    bool IsCertified,
    DateTime? CertifiedAtUtc);

public sealed record ChangeDutyStatusRequest(
    DutyStatus Status,
    Guid? VehicleId = null,
    decimal? Odometer = null,
    string? LocationName = null,
    double? Latitude = null,
    double? Longitude = null,
    string? Notes = null);

public sealed record HosViolationDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    HosViolationType ViolationType,
    string ViolationTypeName,
    DateTime OccurredAtUtc,
    int DurationMinutes,
    string? Notes,
    bool IsAcknowledged,
    DateTime? AcknowledgedAtUtc);

public sealed record HosDriverClocksDto(
    Guid DriverId,
    string DriverName,
    DutyStatus CurrentStatus,
    string CurrentStatusName,
    DateTime CurrentStatusStartedAtUtc,
    double RemainingDrivingHours,
    double RemainingShiftDutyHours,
    double HoursUntilBreakRequired,
    double CycleHoursUsed,
    double CycleHoursRemaining,
    HosRuleConfigurationDto ActivePolicy,
    List<HosViolationDto> ActiveViolations);

public sealed record HosLogFilterRequest(
    Guid? DriverId = null,
    DateTime? Date = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record HosViolationFilterRequest(
    Guid? DriverId = null,
    HosViolationType? ViolationType = null,
    bool? IsAcknowledged = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record HosRoadsideReportDto(
    Guid DriverId,
    string DriverName,
    string DriverLicenseNumber,
    DateTime DateUtc,
    string CurrentVehiclePlate,
    HosRuleConfigurationDto AppliedPolicy,
    List<HosLogEntryDto> DayLogs,
    List<HosViolationDto> DayViolations,
    double TotalDrivingHours,
    double TotalOnDutyHours,
    double TotalOffDutyHours,
    double TotalSleeperHours);

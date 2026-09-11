using ConnectedOps.Domain.FleetOperations;

namespace ConnectedOps.Application.FleetOperations;

public sealed record FleetShiftDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    Guid? BranchId,
    string? BranchName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool CrossesMidnight,
    DayOfWeekFlags DaysOfWeek,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateFleetShiftRequest(
    string Name,
    string Code,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DayOfWeekFlags DaysOfWeek = DayOfWeekFlags.All,
    Guid? BranchId = null,
    string? Description = null);

public sealed record UpdateFleetShiftRequest(
    string Name,
    string Code,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DayOfWeekFlags DaysOfWeek,
    Guid? BranchId,
    string? Description);

public sealed record FleetShiftAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid FleetShiftId,
    string ShiftName,
    string ShiftCode,
    DateOnly AssignmentDate,
    DateTime StartDateTimeUtc,
    DateTime? EndDateTimeUtc,
    Guid? DriverId,
    string? DriverNumber,
    string? DriverDisplayName,
    Guid? VehicleId,
    string? VehicleNumber,
    string? VehicleDisplayName,
    ShiftAssignmentStatus AssignmentStatus,
    string AssignmentStatusName,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateShiftAssignmentRequest(
    Guid FleetShiftId,
    DateOnly AssignmentDate,
    DateTime StartDateTimeUtc,
    DateTime? EndDateTimeUtc = null,
    Guid? DriverId = null,
    Guid? VehicleId = null,
    string? Notes = null);

public sealed record UpdateShiftAssignmentRequest(
    Guid? DriverId,
    Guid? VehicleId,
    DateTime StartDateTimeUtc,
    DateTime? EndDateTimeUtc,
    string? Notes);

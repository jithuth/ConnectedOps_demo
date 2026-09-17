using ConnectedOps.Domain.Inspections;

namespace ConnectedOps.Application.Inspections;

public sealed record DvirFilterRequest(
    DvirStatus? Status = null,
    DvirType? InspectionType = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record DvirInspectionDto(
    Guid Id,
    string InspectionNumber,
    Guid VehicleId,
    string VehiclePlateNumber,
    string VehicleMakeModel,
    Guid DriverId,
    string DriverName,
    DvirType InspectionType,
    string InspectionTypeName,
    DvirStatus Status,
    string StatusName,
    decimal Odometer,
    string? LocationName,
    DateTime InspectedAtUtc,
    string? Remarks,
    string? DriverSignatureData,
    string? MechanicName,
    string? MechanicNotes,
    DateTime? CertifiedAtUtc,
    List<DvirItemCheckDto> Items);

public sealed record DvirItemCheckDto(
    Guid Id,
    string Category,
    string ItemName,
    bool IsPassed,
    DefectSeverity? Severity,
    string? SeverityName,
    string? DefectDescription);

public sealed record CreateDvirRequest(
    Guid VehicleId,
    Guid DriverId,
    DvirType InspectionType,
    decimal Odometer,
    string? LocationName,
    string? DriverSignatureData,
    string? Remarks,
    List<CreateDvirItemCheckRequest> Items);

public sealed record CreateDvirItemCheckRequest(
    string Category,
    string ItemName,
    bool IsPassed,
    DefectSeverity? Severity = null,
    string? DefectDescription = null);

public sealed record SignOffDvirRequest(
    string MechanicName,
    string? MechanicNotes,
    string? MechanicSignatureData);

public sealed record DvirDashboardDto(
    int TotalInspectionsToday,
    int GroundedVehiclesCount,
    int ActiveDefectsCount,
    int CertifiedTodayCount,
    List<DvirInspectionDto> RecentInspections);

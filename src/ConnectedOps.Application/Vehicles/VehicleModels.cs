using ConnectedOps.Domain.Vehicles;
using DriveType = ConnectedOps.Domain.Vehicles.DriveType;

namespace ConnectedOps.Application.Vehicles;

public sealed record VehicleListItemDto(
    Guid Id,
    string VehicleNumber,
    string DisplayName,
    string? RegistrationNumber,
    string? VIN,
    Guid VehicleCategoryId,
    string CategoryName,
    Guid VehicleMakeId,
    string MakeName,
    Guid VehicleModelId,
    string ModelName,
    int? ModelYear,
    FuelType FuelType,
    string FuelTypeName,
    TransmissionType TransmissionType,
    string TransmissionTypeName,
    OwnershipType OwnershipType,
    string OwnershipTypeName,
    VehicleStatus Status,
    string StatusName,
    Guid? BranchId,
    string? BranchName,
    Guid? LocationId,
    string? LocationName,
    decimal CurrentOdometer,
    OdometerUnit OdometerUnit,
    bool IsActive,
    string? PrimaryImageObjectKey,
    DateTime CreatedAtUtc,
    int DocumentCount,
    int ExpiringDocumentCount);

public sealed record VehicleDetailDto(
    Guid Id,
    Guid TenantId,
    string VehicleNumber,
    string DisplayName,
    string? InternalCode,
    string? RegistrationNumber,
    string? VIN,
    string? ChassisNumber,
    string? EngineNumber,
    Guid VehicleCategoryId,
    string CategoryName,
    Guid VehicleMakeId,
    string MakeName,
    Guid VehicleModelId,
    string ModelName,
    int? ModelYear,
    int? ManufactureYear,
    FuelType FuelType,
    string FuelTypeName,
    TransmissionType TransmissionType,
    string TransmissionTypeName,
    string? Color,
    int? NumberOfSeats,
    decimal? GrossVehicleWeight,
    decimal? PayloadCapacity,
    OwnershipType OwnershipType,
    string OwnershipTypeName,
    string? OwnerName,
    string? LeaseCompany,
    DateOnly? LeaseStartDate,
    DateOnly? LeaseEndDate,
    decimal? MonthlyLeaseCost,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    Guid? BranchId,
    string? BranchName,
    Guid? LocationId,
    string? LocationName,
    VehicleStatus Status,
    string StatusName,
    DateOnly? InServiceDate,
    DateOnly? OutOfServiceDate,
    bool IsActive,
    decimal CurrentOdometer,
    OdometerUnit OdometerUnit,
    string? PrimaryImageObjectKey,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    VehicleSpecificationDto? Specification,
    VehicleRegistrationDto? CurrentRegistration,
    IReadOnlyCollection<VehicleRegistrationDto> Registrations,
    IReadOnlyCollection<VehicleOdometerEntryDto> RecentOdometerEntries,
    IReadOnlyCollection<VehicleDocumentDto> Documents,
    IReadOnlyCollection<VehicleNoteDto> NotesList);

public sealed record CreateVehicleRequest(
    string VehicleNumber,
    Guid CategoryId,
    Guid MakeId,
    Guid ModelId,
    string? DisplayName,
    string? InternalCode,
    string? RegistrationNumber,
    string? VIN,
    string? ChassisNumber,
    string? EngineNumber,
    int? ModelYear,
    int? ManufactureYear,
    FuelType FuelType,
    TransmissionType TransmissionType,
    OwnershipType OwnershipType,
    Guid? BranchId,
    Guid? LocationId,
    decimal InitialOdometer,
    OdometerUnit OdometerUnit,
    VehicleStatus Status,
    string? Color,
    int? NumberOfSeats,
    decimal? GrossVehicleWeight,
    decimal? PayloadCapacity,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    DateOnly? InServiceDate,
    string? OwnerName,
    string? LeaseCompany,
    DateOnly? LeaseStartDate,
    DateOnly? LeaseEndDate,
    decimal? MonthlyLeaseCost,
    string? Notes);

public sealed record UpdateVehicleRequest(
    string VehicleNumber,
    Guid CategoryId,
    Guid MakeId,
    Guid ModelId,
    string? DisplayName,
    string? InternalCode,
    string? RegistrationNumber,
    string? VIN,
    string? ChassisNumber,
    string? EngineNumber,
    int? ModelYear,
    int? ManufactureYear,
    FuelType FuelType,
    TransmissionType TransmissionType,
    OwnershipType OwnershipType,
    Guid? BranchId,
    Guid? LocationId,
    string? Color,
    int? NumberOfSeats,
    decimal? GrossVehicleWeight,
    decimal? PayloadCapacity,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    string? OwnerName,
    string? LeaseCompany,
    DateOnly? LeaseStartDate,
    DateOnly? LeaseEndDate,
    decimal? MonthlyLeaseCost,
    string? Notes);

public sealed record ChangeVehicleStatusRequest(
    VehicleStatus NewStatus,
    string? Notes);

public sealed record AssignVehicleBranchRequest(
    Guid? BranchId);

public sealed record AssignVehicleLocationRequest(
    Guid? LocationId);

public sealed record VehicleSpecificationDto(
    Guid VehicleId,
    int? EngineCapacityCc,
    decimal? EnginePowerKw,
    int? CylinderCount,
    decimal? FuelTankCapacity,
    decimal? BatteryVoltage,
    decimal? LengthMm,
    decimal? WidthMm,
    decimal? HeightMm,
    decimal? GrossVehicleWeightKg,
    decimal? KerbWeightKg,
    decimal? PayloadCapacityKg,
    int? AxleCount,
    int? WheelCount,
    int? SeatCount,
    string? BodyType,
    DriveType DriveType,
    string DriveTypeName,
    string? EmissionStandard,
    string? TyreSizeFront,
    string? TyreSizeRear);

public sealed record UpsertVehicleSpecificationRequest(
    int? EngineCapacityCc,
    decimal? EnginePowerKw,
    int? CylinderCount,
    decimal? FuelTankCapacity,
    decimal? BatteryVoltage,
    decimal? LengthMm,
    decimal? WidthMm,
    decimal? HeightMm,
    decimal? GrossVehicleWeightKg,
    decimal? KerbWeightKg,
    decimal? PayloadCapacityKg,
    int? AxleCount,
    int? WheelCount,
    int? SeatCount,
    string? BodyType,
    DriveType DriveType,
    string? EmissionStandard,
    string? TyreSizeFront,
    string? TyreSizeRear);

public sealed record VehicleRegistrationDto(
    Guid Id,
    Guid VehicleId,
    string RegistrationNumber,
    string? RegistrationCountryCode,
    string? RegistrationStateProvince,
    DateOnly? RegistrationDate,
    DateOnly? ExpiryDate,
    string? IssuingAuthority,
    bool IsCurrent,
    string? Notes,
    bool IsExpired);

public sealed record CreateVehicleRegistrationRequest(
    string RegistrationNumber,
    string? RegistrationCountryCode,
    string? RegistrationStateProvince,
    DateOnly? RegistrationDate,
    DateOnly? ExpiryDate,
    string? IssuingAuthority,
    string? Notes);

public sealed record VehicleOdometerEntryDto(
    Guid Id,
    Guid VehicleId,
    decimal Reading,
    OdometerUnit Unit,
    string UnitName,
    DateTime ReadingDateUtc,
    OdometerSource Source,
    string SourceName,
    string? Notes,
    Guid? RecordedByUserId,
    DateTime CreatedAtUtc);

public sealed record RecordVehicleOdometerRequest(
    decimal Reading,
    OdometerUnit Unit,
    DateTime ReadingDateUtc,
    OdometerSource Source,
    string? Notes);

public sealed record VehicleDocumentDto(
    Guid Id,
    Guid VehicleId,
    VehicleDocumentType DocumentType,
    string DocumentTypeName,
    string Title,
    string? DocumentNumber,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long? FileSizeBytes,
    bool IsExpired,
    bool IsExpiringSoon,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateVehicleDocumentRequest(
    VehicleDocumentType DocumentType,
    string Title,
    string? DocumentNumber,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long? FileSizeBytes,
    string? Notes);

public sealed record UpdateVehicleDocumentRequest(
    VehicleDocumentType DocumentType,
    string Title,
    string? DocumentNumber,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? Notes);

public sealed record VehicleNoteDto(
    Guid Id,
    Guid VehicleId,
    string NoteText,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAtUtc);

public sealed record CreateVehicleNoteRequest(
    string NoteText);

public sealed record VehicleQueryParameters
{
    public string? SearchTerm { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? MakeId { get; init; }
    public Guid? ModelId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? LocationId { get; init; }
    public VehicleStatus? Status { get; init; }
    public FuelType? FuelType { get; init; }
    public OwnershipType? OwnershipType { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

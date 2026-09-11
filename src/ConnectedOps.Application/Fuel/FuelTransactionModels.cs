using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelTransactionDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid? DriverId,
    string? DriverName,
    Guid? FuelStationId,
    string? FuelStationName,
    string? FuelStationCode,
    Guid? FuelCardId,
    string? FuelCardNumberMasked,
    FuelType FuelType,
    string FuelTypeName,
    DateTime TransactionDateUtc,
    decimal? OdometerReading,
    OdometerUnit? OdometerUnit,
    decimal? EngineHours,
    decimal Quantity,
    FuelUnit QuantityUnit,
    string QuantityUnitName,
    decimal UnitPrice,
    decimal TotalCost,
    string CurrencyCode,
    bool IsFullTank,
    bool IsPartialFill,
    FuelTransactionStatus Status,
    string StatusName,
    FuelTransactionSource Source,
    string SourceName,
    string? TransactionReference,
    string? ReceiptNumber,
    string? PaymentMethod,
    string? ExternalTransactionId,
    double? Latitude,
    double? Longitude,
    string? Notes,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    FuelEfficiencyDto? Efficiency,
    IReadOnlyCollection<FuelTransactionDocumentDto> Documents,
    IReadOnlyCollection<FuelAnomalyDto> Anomalies);

public sealed record FuelTransactionListItemDto(
    Guid Id,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid? DriverId,
    string? DriverName,
    Guid? FuelStationId,
    string? FuelStationName,
    FuelType FuelType,
    string FuelTypeName,
    DateTime TransactionDateUtc,
    decimal? OdometerReading,
    OdometerUnit? OdometerUnit,
    decimal Quantity,
    FuelUnit QuantityUnit,
    decimal UnitPrice,
    decimal TotalCost,
    string CurrencyCode,
    bool IsFullTank,
    bool IsPartialFill,
    FuelTransactionStatus Status,
    string StatusName,
    FuelTransactionSource Source,
    string SourceName,
    string? ReceiptNumber,
    string? TransactionReference,
    decimal? KilometersPerLiter,
    decimal? LitersPer100Km,
    int AnomalyCount,
    int DocumentCount,
    DateTime CreatedAtUtc);

public sealed record CreateFuelTransactionRequest(
    Guid VehicleId,
    DateTime TransactionDateUtc,
    decimal Quantity,
    decimal UnitPrice,
    decimal? TotalCost = null,
    FuelType FuelType = FuelType.Diesel,
    FuelUnit QuantityUnit = FuelUnit.Liter,
    string? CurrencyCode = "USD",
    bool IsFullTank = true,
    bool IsPartialFill = false,
    decimal? OdometerReading = null,
    OdometerUnit? OdometerUnit = null,
    decimal? EngineHours = null,
    Guid? DriverId = null,
    Guid? FuelStationId = null,
    Guid? FuelCardId = null,
    FuelTransactionSource Source = FuelTransactionSource.Manual,
    string? TransactionReference = null,
    string? ReceiptNumber = null,
    string? PaymentMethod = null,
    string? ExternalTransactionId = null,
    double? Latitude = null,
    double? Longitude = null,
    string? Notes = null);

public sealed record UpdateFuelTransactionRequest(
    Guid VehicleId,
    DateTime TransactionDateUtc,
    decimal Quantity,
    decimal UnitPrice,
    decimal? TotalCost,
    FuelType FuelType,
    FuelUnit QuantityUnit,
    string? CurrencyCode,
    bool IsFullTank,
    bool IsPartialFill,
    decimal? OdometerReading,
    OdometerUnit? OdometerUnit,
    decimal? EngineHours,
    Guid? DriverId,
    Guid? FuelStationId,
    Guid? FuelCardId,
    string? TransactionReference,
    string? ReceiptNumber,
    string? PaymentMethod,
    double? Latitude,
    double? Longitude,
    string? Notes);

public sealed record CancelFuelTransactionRequest(
    string? CancellationReason = null);

public sealed record FuelTransactionQueryParameters
{
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public Guid? FuelStationId { get; init; }
    public Guid? FuelCardId { get; init; }
    public Guid? BranchId { get; init; }
    public FuelType? FuelType { get; init; }
    public FuelTransactionStatus? Status { get; init; }
    public FuelTransactionSource? Source { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

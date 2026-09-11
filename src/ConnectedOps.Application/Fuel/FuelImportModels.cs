using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelImportBatchDto(
    Guid Id,
    FuelTransactionSource Source,
    string SourceName,
    string? FileName,
    string? ProviderName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int TotalRows,
    int ImportedRows,
    int RejectedRows,
    FuelImportStatus Status,
    string StatusName,
    IReadOnlyCollection<FuelImportErrorDto> Errors);

public sealed record FuelImportErrorDto(
    Guid Id,
    int? RowNumber,
    string? ExternalReference,
    string ErrorCode,
    string ErrorMessage,
    DateTime CreatedAtUtc);

public sealed record ImportFuelTransactionsRequest(
    FuelTransactionSource Source = FuelTransactionSource.Imported,
    string? FileName = null,
    string? ProviderName = null,
    IReadOnlyCollection<ImportFuelTransactionRowRequest>? Rows = null);

public sealed record ImportFuelTransactionRowRequest(
    int RowNumber,
    string VehicleNumberOrRegistration,
    DateTime TransactionDateUtc,
    decimal Quantity,
    decimal UnitPrice,
    decimal? TotalCost = null,
    FuelType FuelType = FuelType.Diesel,
    FuelUnit QuantityUnit = FuelUnit.Liter,
    string? CurrencyCode = "USD",
    bool IsFullTank = true,
    decimal? OdometerReading = null,
    OdometerUnit? OdometerUnit = null,
    decimal? EngineHours = null,
    string? DriverNumberOrEmail = null,
    string? StationCodeOrName = null,
    string? CardNumberOrReference = null,
    string? ExternalTransactionId = null,
    string? ReceiptNumber = null,
    string? Notes = null);

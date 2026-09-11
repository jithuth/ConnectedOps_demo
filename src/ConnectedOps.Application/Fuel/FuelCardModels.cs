using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelCardDto(
    Guid Id,
    Guid TenantId,
    string CardNumberMasked,
    string CardReference,
    string ProviderName,
    Guid? VehicleId,
    string? VehicleNumber,
    string? VehicleDisplayName,
    Guid? DriverId,
    string? DriverName,
    DateTime? IssuedAtUtc,
    DateTime? ExpiresAtUtc,
    FuelCardStatus Status,
    string StatusName,
    decimal? SpendingLimit,
    string CurrencyCode,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record FuelCardListItemDto(
    Guid Id,
    string CardNumberMasked,
    string CardReference,
    string ProviderName,
    Guid? VehicleId,
    string? VehicleNumber,
    Guid? DriverId,
    string? DriverName,
    DateTime? ExpiresAtUtc,
    FuelCardStatus Status,
    string StatusName,
    decimal? SpendingLimit,
    string CurrencyCode,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateFuelCardRequest(
    string CardNumber,
    string CardReference,
    string ProviderName,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    DateTime? IssuedAtUtc = null,
    DateTime? ExpiresAtUtc = null,
    FuelCardStatus Status = FuelCardStatus.Active,
    decimal? SpendingLimit = null,
    string? CurrencyCode = "USD",
    string? Notes = null,
    bool IsActive = true);

public sealed record UpdateFuelCardRequest(
    string CardNumber,
    string CardReference,
    string ProviderName,
    Guid? VehicleId,
    Guid? DriverId,
    DateTime? IssuedAtUtc,
    DateTime? ExpiresAtUtc,
    FuelCardStatus Status,
    decimal? SpendingLimit,
    string? CurrencyCode,
    string? Notes,
    bool IsActive);

public sealed record UpdateFuelCardStatusRequest(
    FuelCardStatus Status);

public sealed record FuelCardQueryParameters
{
    public FuelCardStatus? Status { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public bool? ActiveOnly { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

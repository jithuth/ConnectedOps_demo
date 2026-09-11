using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelCard : BaseEntity
{
    private FuelCard()
    {
    }

    public FuelCard(
        Guid tenantId,
        string cardNumberMasked,
        string cardReference,
        string providerName,
        Guid? vehicleId = null,
        Guid? driverId = null,
        DateTime? issuedAtUtc = null,
        DateTime? expiresAtUtc = null,
        FuelCardStatus status = FuelCardStatus.Active,
        decimal? spendingLimit = null,
        string? currencyCode = "USD",
        string? notes = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(cardNumberMasked))
            throw new ArgumentException("Masked card number is required.", nameof(cardNumberMasked));
        if (string.IsNullOrWhiteSpace(cardReference))
            throw new ArgumentException("Card reference is required.", nameof(cardReference));
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required.", nameof(providerName));

        TenantId = tenantId;
        CardNumberMasked = MaskCardNumber(cardNumberMasked);
        CardReference = cardReference.Trim();
        ProviderName = providerName.Trim();
        VehicleId = vehicleId;
        DriverId = driverId;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = status;
        SpendingLimit = spendingLimit;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        Notes = notes?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string CardNumberMasked { get; private set; } = string.Empty;
    public string CardReference { get; private set; } = string.Empty;
    public string ProviderName { get; private set; } = string.Empty;
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public FuelCardStatus Status { get; private set; }
    public decimal? SpendingLimit { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string cardNumberMasked,
        string cardReference,
        string providerName,
        Guid? vehicleId,
        Guid? driverId,
        DateTime? issuedAtUtc,
        DateTime? expiresAtUtc,
        FuelCardStatus status,
        decimal? spendingLimit,
        string? currencyCode,
        string? notes,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(cardNumberMasked))
            throw new ArgumentException("Masked card number is required.", nameof(cardNumberMasked));
        if (string.IsNullOrWhiteSpace(cardReference))
            throw new ArgumentException("Card reference is required.", nameof(cardReference));
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required.", nameof(providerName));

        CardNumberMasked = MaskCardNumber(cardNumberMasked);
        CardReference = cardReference.Trim();
        ProviderName = providerName.Trim();
        VehicleId = vehicleId;
        DriverId = driverId;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = status;
        SpendingLimit = spendingLimit;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        Notes = notes?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    public void SetStatus(FuelCardStatus status, Guid? updatedBy = null)
    {
        Status = status;
        IsActive = status == FuelCardStatus.Active;
        MarkUpdated(updatedBy);
    }

    public static string MaskCardNumber(string rawOrMasked)
    {
        var clean = rawOrMasked.Trim().Replace(" ", "").Replace("-", "");
        if (clean.Length <= 4)
            return $"****-****-****-{clean}";
        
        var last4 = clean[^4..];
        return $"****-****-****-{last4}";
    }
}

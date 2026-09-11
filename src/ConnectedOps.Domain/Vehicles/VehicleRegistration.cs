using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleRegistration : BaseEntity
{
    private VehicleRegistration()
    {
    }

    public VehicleRegistration(
        Guid tenantId,
        Guid vehicleId,
        string registrationNumber,
        string? registrationCountryCode = null,
        string? registrationStateProvince = null,
        DateOnly? registrationDate = null,
        DateOnly? expiryDate = null,
        string? issuingAuthority = null,
        bool isCurrent = true,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("RegistrationNumber is required.", nameof(registrationNumber));

        TenantId = tenantId;
        VehicleId = vehicleId;
        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
        RegistrationCountryCode = registrationCountryCode?.Trim().ToUpperInvariant();
        RegistrationStateProvince = registrationStateProvince?.Trim().ToUpperInvariant();
        RegistrationDate = registrationDate;
        ExpiryDate = expiryDate;
        IssuingAuthority = issuingAuthority?.Trim();
        IsCurrent = isCurrent;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public string RegistrationNumber { get; private set; } = string.Empty;
    public string? RegistrationCountryCode { get; private set; }
    public string? RegistrationStateProvince { get; private set; }
    public DateOnly? RegistrationDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? IssuingAuthority { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? Notes { get; private set; }

    public void MarkNotCurrent()
    {
        IsCurrent = false;
        MarkUpdated();
    }

    public void SetCurrent()
    {
        IsCurrent = true;
        MarkUpdated();
    }

    public bool IsExpired(DateOnly? asOfDate = null)
    {
        if (!ExpiryDate.HasValue)
            return false;

        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpiryDate.Value < referenceDate;
    }
}

using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleMake : BaseEntity
{
    private readonly List<VehicleModel> _models = [];

    private VehicleMake()
    {
    }

    public VehicleMake(
        Guid tenantId,
        string name,
        string? countryCode = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? CountryCode { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<VehicleModel> Models => _models.AsReadOnly();

    public void Update(string name, string? countryCode)
    {
        SetName(name);
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Make name is required.", nameof(name));

        Name = name.Trim();
    }
}

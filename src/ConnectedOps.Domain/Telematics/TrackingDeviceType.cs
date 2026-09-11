using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class TrackingDeviceType : BaseEntity
{
    private TrackingDeviceType()
    {
    }

    public TrackingDeviceType(
        string name,
        string code,
        string? description = null,
        Guid? tenantId = null,
        bool supportsGps = true,
        bool supportsIgnition = true,
        bool supportsCanBus = false,
        bool supportsObd = false,
        bool supportsBattery = true,
        bool supportsTemperature = false,
        bool supportsFuel = false,
        bool supportsBle = false,
        bool supportsCommands = false,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Device type name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Device type code is required.", nameof(code));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        TenantId = tenantId;
        SupportsGps = supportsGps;
        SupportsIgnition = supportsIgnition;
        SupportsCanBus = supportsCanBus;
        SupportsObd = supportsObd;
        SupportsBattery = supportsBattery;
        SupportsTemperature = supportsTemperature;
        SupportsFuel = supportsFuel;
        SupportsBle = supportsBle;
        SupportsCommands = supportsCommands;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool SupportsGps { get; private set; } = true;
    public bool SupportsIgnition { get; private set; } = true;
    public bool SupportsCanBus { get; private set; }
    public bool SupportsObd { get; private set; }
    public bool SupportsBattery { get; private set; } = true;
    public bool SupportsTemperature { get; private set; }
    public bool SupportsFuel { get; private set; }
    public bool SupportsBle { get; private set; }
    public bool SupportsCommands { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(
        string name,
        string? description,
        bool supportsGps,
        bool supportsIgnition,
        bool supportsCanBus,
        bool supportsObd,
        bool supportsBattery,
        bool supportsTemperature,
        bool supportsFuel,
        bool supportsBle,
        bool supportsCommands,
        Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Device type name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        SupportsGps = supportsGps;
        SupportsIgnition = supportsIgnition;
        SupportsCanBus = supportsCanBus;
        SupportsObd = supportsObd;
        SupportsBattery = supportsBattery;
        SupportsTemperature = supportsTemperature;
        SupportsFuel = supportsFuel;
        SupportsBle = supportsBle;
        SupportsCommands = supportsCommands;
        MarkUpdated(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        MarkUpdated(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        MarkUpdated(userId);
    }
}

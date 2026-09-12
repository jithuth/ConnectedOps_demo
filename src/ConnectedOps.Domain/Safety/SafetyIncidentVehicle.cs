using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncidentVehicle : BaseEntity
{
    private SafetyIncidentVehicle()
    {
    }

    public SafetyIncidentVehicle(
        Guid tenantId,
        Guid safetyIncidentId,
        Guid vehicleId,
        bool damageReported = false,
        string? damageDescription = null,
        bool isPrimaryVehicle = false,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (safetyIncidentId == Guid.Empty)
            throw new ArgumentException("SafetyIncidentId is required.", nameof(safetyIncidentId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        SafetyIncidentId = safetyIncidentId;
        VehicleId = vehicleId;
        DamageReported = damageReported;
        DamageDescription = damageDescription?.Trim();
        IsPrimaryVehicle = isPrimaryVehicle;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid SafetyIncidentId { get; private set; }
    public SafetyIncident SafetyIncident { get; private set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public bool DamageReported { get; private set; }
    public string? DamageDescription { get; private set; }
    public bool IsPrimaryVehicle { get; private set; }

    public void Update(
        bool damageReported,
        string? damageDescription,
        bool isPrimaryVehicle,
        Guid? updatedByUserId = null)
    {
        DamageReported = damageReported;
        DamageDescription = damageDescription?.Trim();
        IsPrimaryVehicle = isPrimaryVehicle;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }
}

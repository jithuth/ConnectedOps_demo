using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetVehicleAssignment : BaseEntity
{
    private AssetVehicleAssignment()
    {
    }

    public AssetVehicleAssignment(
        Guid tenantId,
        Guid assetId,
        Guid vehicleId,
        DateTime assignedFromUtc,
        DateTime? assignedToUtc = null,
        string? notes = null,
        bool isActive = true,
        Guid? assignedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        AssetId = assetId;
        VehicleId = vehicleId;
        AssignedFromUtc = assignedFromUtc;
        AssignedToUtc = assignedToUtc;
        Notes = notes?.Trim();
        IsActive = isActive;
        AssignedByUserId = assignedByUserId;
        CreatedBy = assignedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public DateTime AssignedFromUtc { get; private set; }
    public DateTime? AssignedToUtc { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public Guid? EndedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public void EndAssignment(
        DateTime assignedToUtc,
        string? returnNotes = null,
        Guid? endedByUserId = null)
    {
        AssignedToUtc = assignedToUtc;
        if (!string.IsNullOrWhiteSpace(returnNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? returnNotes.Trim() : $"{Notes} | Removal notes: {returnNotes.Trim()}";
        }
        IsActive = false;
        EndedByUserId = endedByUserId;
        MarkUpdated(endedByUserId);
    }
}

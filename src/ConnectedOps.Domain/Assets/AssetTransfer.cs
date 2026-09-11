using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetTransfer : BaseEntity
{
    private AssetTransfer()
    {
    }

    public AssetTransfer(
        Guid tenantId,
        Guid assetId,
        AssetTransferType transferType,
        Guid? fromBranchId = null,
        Guid? toBranchId = null,
        Guid? fromLocationId = null,
        Guid? toLocationId = null,
        Guid? fromEmployeeId = null,
        Guid? toEmployeeId = null,
        Guid? fromVehicleId = null,
        Guid? toVehicleId = null,
        string? reason = null,
        string? notes = null,
        DateTime? requestedAtUtc = null,
        Guid? requestedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        TransferType = transferType;
        FromBranchId = fromBranchId;
        ToBranchId = toBranchId;
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        FromEmployeeId = fromEmployeeId;
        ToEmployeeId = toEmployeeId;
        FromVehicleId = fromVehicleId;
        ToVehicleId = toVehicleId;
        Reason = reason?.Trim();
        Notes = notes?.Trim();
        RequestedAtUtc = requestedAtUtc ?? DateTime.UtcNow;
        Status = AssetTransferStatus.Pending;
        RequestedByUserId = requestedByUserId;
        CreatedBy = requestedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public AssetTransferType TransferType { get; private set; }
    public AssetTransferStatus Status { get; private set; }

    public Guid? FromBranchId { get; private set; }
    public Branch? FromBranch { get; private set; }
    public Guid? ToBranchId { get; private set; }
    public Branch? ToBranch { get; private set; }

    public Guid? FromLocationId { get; private set; }
    public Location? FromLocation { get; private set; }
    public Guid? ToLocationId { get; private set; }
    public Location? ToLocation { get; private set; }

    public Guid? FromEmployeeId { get; private set; }
    public Employee? FromEmployee { get; private set; }
    public Guid? ToEmployeeId { get; private set; }
    public Employee? ToEmployee { get; private set; }

    public Guid? FromVehicleId { get; private set; }
    public Vehicle? FromVehicle { get; private set; }
    public Guid? ToVehicleId { get; private set; }
    public Vehicle? ToVehicle { get; private set; }

    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public Guid? CompletedByUserId { get; private set; }

    public void MarkInTransit(Guid? updatedBy = null)
    {
        if (Status != AssetTransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be marked in-transit.");
        Status = AssetTransferStatus.InTransit;
        MarkUpdated(updatedBy);
    }

    public void Complete(string? completionNotes = null, Guid? completedByUserId = null)
    {
        if (Status == AssetTransferStatus.Completed || Status == AssetTransferStatus.Cancelled)
            throw new InvalidOperationException("Transfer has already been finalized.");

        Status = AssetTransferStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        CompletedByUserId = completedByUserId;
        if (!string.IsNullOrWhiteSpace(completionNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? completionNotes.Trim() : $"{Notes} | Completion: {completionNotes.Trim()}";
        }
        MarkUpdated(completedByUserId);
    }

    public void Cancel(string? cancellationReason = null, Guid? cancelledByUserId = null)
    {
        if (Status == AssetTransferStatus.Completed || Status == AssetTransferStatus.Cancelled)
            throw new InvalidOperationException("Transfer has already been finalized.");

        Status = AssetTransferStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(cancellationReason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? cancellationReason.Trim() : $"{Notes} | Cancelled: {cancellationReason.Trim()}";
        }
        MarkUpdated(cancelledByUserId);
    }
}

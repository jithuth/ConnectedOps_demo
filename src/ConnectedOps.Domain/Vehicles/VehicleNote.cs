using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleNote : BaseEntity
{
    private VehicleNote()
    {
    }

    public VehicleNote(
        Guid tenantId,
        Guid vehicleId,
        string noteText,
        Guid? createdByUserId = null,
        string? createdByUserName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required.", nameof(noteText));

        TenantId = tenantId;
        VehicleId = vehicleId;
        NoteText = noteText.Trim();
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public string NoteText { get; private set; } = string.Empty;
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }

    public void UpdateText(string noteText)
    {
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required.", nameof(noteText));

        NoteText = noteText.Trim();
        MarkUpdated();
    }
}

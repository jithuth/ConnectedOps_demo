using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverNote : BaseEntity
{
    private DriverNote()
    {
    }

    public DriverNote(
        Guid tenantId,
        Guid driverId,
        string noteText,
        Guid? createdByUserId = null,
        string? createdByUserName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required.", nameof(noteText));

        TenantId = tenantId;
        DriverId = driverId;
        NoteText = noteText.Trim();
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public string NoteText { get; private set; } = string.Empty;
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
}

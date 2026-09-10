namespace ConnectedOps.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    public Guid? CreatedBy { get; protected set; }

    public Guid? UpdatedBy { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAtUtc { get; protected set; }

    public void MarkUpdated(Guid? userId = null)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = userId;
    }

    public void SoftDelete(Guid? userId = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
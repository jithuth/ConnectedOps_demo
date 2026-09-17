using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Domain.Gamification;

public sealed class DriverBadge : BaseEntity
{
    private DriverBadge()
    {
    }

    public DriverBadge(
        Guid tenantId,
        Guid driverId,
        BadgeType badgeType,
        string title,
        string description,
        int points = 100,
        DateTime? earnedAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        TenantId = tenantId;
        DriverId = driverId;
        BadgeType = badgeType;
        Title = title.Trim();
        Description = description.Trim();
        Points = Math.Max(0, points);
        EarnedAtUtc = earnedAtUtc ?? DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public BadgeType BadgeType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Points { get; private set; }
    public DateTime EarnedAtUtc { get; private set; }
}

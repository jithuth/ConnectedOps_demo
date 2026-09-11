using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Demo;

public sealed class DemoRoute : BaseEntity
{
    private readonly List<DemoRoutePoint> _points = [];

    private DemoRoute()
    {
    }

    public DemoRoute(
        Guid tenantId,
        string name,
        string? description = null,
        bool isLoop = true,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Route name is required.", nameof(name));

        TenantId = tenantId;
        Name = name.Trim();
        Description = description?.Trim();
        IsLoop = isLoop;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsLoop { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<DemoRoutePoint> Points => _points.AsReadOnly();

    public void AddPoint(int sequence, double latitude, double longitude, decimal? speedKph = null)
    {
        _points.Add(new DemoRoutePoint(Id, sequence, latitude, longitude, speedKph));
    }

    public void ClearPoints()
    {
        _points.Clear();
    }
}

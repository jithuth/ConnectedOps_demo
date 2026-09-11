using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Demo;

public sealed class DemoRoutePoint : BaseEntity
{
    private DemoRoutePoint()
    {
    }

    public DemoRoutePoint(
        Guid demoRouteId,
        int sequence,
        double latitude,
        double longitude,
        decimal? speedKph = null)
    {
        if (demoRouteId == Guid.Empty)
            throw new ArgumentException("DemoRouteId is required.", nameof(demoRouteId));

        DemoRouteId = demoRouteId;
        Sequence = sequence;
        Latitude = latitude;
        Longitude = longitude;
        SpeedKph = speedKph;
    }

    public Guid DemoRouteId { get; private set; }
    public DemoRoute DemoRoute { get; private set; } = null!;

    public int Sequence { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public decimal? SpeedKph { get; private set; }
}

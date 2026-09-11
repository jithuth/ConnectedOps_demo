using ConnectedOps.Domain.Geofences;
using ConnectedOps.Infrastructure.Geofences;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class GeofenceGeometryTests
{
    [Fact]
    public void CalculateHaversineDistanceMeters_ShouldCalculateAccurateDistance()
    {
        // Dubai Burj Khalifa (25.1972, 55.2744) to Dubai Mall (25.1985, 55.2796)
        double lat1 = 25.1972, lon1 = 55.2744;
        double lat2 = 25.1985, lon2 = 55.2796;

        double distance = GeofenceGeometryHelper.CalculateHaversineDistanceMeters(lat1, lon1, lat2, lon2);

        // Approximately 540-560 meters
        Assert.InRange(distance, 500, 600);
    }

    [Fact]
    public void IsPointInCircle_InsideRadius_ReturnsTrue()
    {
        double centerLat = 25.2048, centerLon = 55.2708;
        double radiusMeters = 1000;

        // Point 200m away
        double pointLat = 25.2055, pointLon = 55.2715;

        bool isInside = GeofenceGeometryHelper.IsPointInCircle(pointLat, pointLon, centerLat, centerLon, radiusMeters);

        Assert.True(isInside);
    }

    [Fact]
    public void IsPointInCircle_OutsideRadius_ReturnsFalse()
    {
        double centerLat = 25.2048, centerLon = 55.2708;
        double radiusMeters = 500;

        // Point ~10km away
        double pointLat = 25.3048, pointLon = 55.3708;

        bool isInside = GeofenceGeometryHelper.IsPointInCircle(pointLat, pointLon, centerLat, centerLon, radiusMeters);

        Assert.False(isInside);
    }

    [Fact]
    public void IsPointInPolygon_InsidePolygon_ReturnsTrue()
    {
        // Simple rectangular polygon around (55.270..55.280 lon, 25.200..25.210 lat)
        var vertices = new List<(double Longitude, double Latitude)>
        {
            (55.270, 25.200),
            (55.280, 25.200),
            (55.280, 25.210),
            (55.270, 25.210),
            (55.270, 25.200) // Closed
        };

        // Point inside rectangle
        double pointLat = 25.205, pointLon = 55.275;

        bool isInside = GeofenceGeometryHelper.IsPointInPolygon(pointLat, pointLon, vertices);

        Assert.True(isInside);
    }

    [Fact]
    public void IsPointInPolygon_OutsidePolygon_ReturnsFalse()
    {
        var vertices = new List<(double Longitude, double Latitude)>
        {
            (55.270, 25.200),
            (55.280, 25.200),
            (55.280, 25.210),
            (55.270, 25.210),
            (55.270, 25.200)
        };

        // Point outside rectangle
        double pointLat = 25.215, pointLon = 55.285;

        bool isInside = GeofenceGeometryHelper.IsPointInPolygon(pointLat, pointLon, vertices);

        Assert.False(isInside);
    }

    [Fact]
    public void ParsePolygonVertices_ValidGeoJson_ExtractsCoordinates()
    {
        // RFC 7946 GeoJSON format: [[[lon, lat], [lon, lat], ...]]
        string geoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[55.270,25.200],[55.280,25.200],[55.280,25.210],[55.270,25.210],[55.270,25.200]]]}";

        var vertices = GeofenceGeometryHelper.ParsePolygonVertices(geoJson);

        Assert.Equal(5, vertices.Count);
        Assert.Equal(25.200, vertices[0].Latitude, 3);
        Assert.Equal(55.270, vertices[0].Longitude, 3);
    }

    [Fact]
    public void ParsePolygonVertices_InvalidOrEmpty_ReturnsEmptyList()
    {
        var vertices1 = GeofenceGeometryHelper.ParsePolygonVertices(null);
        var vertices2 = GeofenceGeometryHelper.ParsePolygonVertices("");
        var vertices3 = GeofenceGeometryHelper.ParsePolygonVertices("invalid json");

        Assert.Empty(vertices1);
        Assert.Empty(vertices2);
        Assert.Empty(vertices3);
    }
}

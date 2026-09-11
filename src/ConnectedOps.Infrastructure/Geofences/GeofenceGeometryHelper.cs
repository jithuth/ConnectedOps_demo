using System.Text.Json;

namespace ConnectedOps.Infrastructure.Geofences;

public static class GeofenceGeometryHelper
{
    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>
    /// Calculates the great-circle distance between two geographic coordinates using the Haversine formula.
    /// </summary>
    public static double CalculateHaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double rLat1 = ToRadians(lat1);
        double rLat2 = ToRadians(lat2);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(rLat1) * Math.Cos(rLat2) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Checks whether a coordinate point is inside a circle geofence.
    /// </summary>
    public static bool IsPointInCircle(double latitude, double longitude, double centerLat, double centerLon, double radiusMeters)
    {
        double distance = CalculateHaversineDistanceMeters(latitude, longitude, centerLat, centerLon);
        return distance <= radiusMeters;
    }

    /// <summary>
    /// Checks whether a coordinate point (latitude, longitude) is inside a polygon using the Ray-Casting algorithm.
    /// Vertices must be (Longitude, Latitude) pairs.
    /// </summary>
    public static bool IsPointInPolygon(double latitude, double longitude, IReadOnlyList<(double Longitude, double Latitude)> vertices)
    {
        if (vertices == null || vertices.Count < 3)
            return false;

        bool inside = false;
        int count = vertices.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            double xi = vertices[i].Longitude, yi = vertices[i].Latitude;
            double xj = vertices[j].Longitude, yj = vertices[j].Latitude;

            // Check if horizontal ray from (longitude, latitude) intersects edge (i, j)
            bool intersect = ((yi > latitude) != (yj > latitude)) &&
                             (longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi);

            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Parses GeoJSON polygon string into a list of (Longitude, Latitude) vertices.
    /// Supports GeoJSON Feature, Polygon object, or raw coordinate array.
    /// </summary>
    public static IReadOnlyList<(double Longitude, double Latitude)> ParsePolygonVertices(string? polygonGeoJson)
    {
        if (string.IsNullOrWhiteSpace(polygonGeoJson))
            return [];

        var vertices = new List<(double Longitude, double Latitude)>();

        try
        {
            using var doc = JsonDocument.Parse(polygonGeoJson);
            var root = doc.RootElement;

            JsonElement coordinatesArray;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("geometry", out var geomProp) && geomProp.TryGetProperty("coordinates", out var fCoords))
                {
                    coordinatesArray = fCoords[0];
                }
                else if (root.TryGetProperty("coordinates", out var pCoords))
                {
                    coordinatesArray = pCoords[0];
                }
                else
                {
                    return [];
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                // Could be [[[lon,lat],...]] or [[lon,lat],...]
                if (root.GetArrayLength() > 0 && root[0].ValueKind == JsonValueKind.Array && root[0].GetArrayLength() > 0 && root[0][0].ValueKind == JsonValueKind.Array)
                {
                    coordinatesArray = root[0];
                }
                else
                {
                    coordinatesArray = root;
                }
            }
            else
            {
                return [];
            }

            foreach (var item in coordinatesArray.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                {
                    double lon = item[0].GetDouble();
                    double lat = item[1].GetDouble();
                    vertices.Add((lon, lat));
                }
            }
        }
        catch
        {
            return [];
        }

        return vertices;
    }

    /// <summary>
    /// Calculates approximate centroid of polygon vertices.
    /// </summary>
    public static (double Latitude, double Longitude)? CalculateCentroid(IReadOnlyList<(double Longitude, double Latitude)> vertices)
    {
        if (vertices == null || vertices.Count == 0)
            return null;

        double sumLat = 0;
        double sumLon = 0;

        foreach (var v in vertices)
        {
            sumLat += v.Latitude;
            sumLon += v.Longitude;
        }

        return (sumLat / vertices.Count, sumLon / vertices.Count);
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}

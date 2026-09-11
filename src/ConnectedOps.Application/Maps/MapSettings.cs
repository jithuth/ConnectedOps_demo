namespace ConnectedOps.Application.Maps;

public sealed class MapSettings
{
    public const string SectionName = "Maps";

    public string Provider { get; set; } = "OpenFreeMap";
    public string StyleUrl { get; set; } = "https://tiles.openfreemap.org/styles/bright";
    public string? TileUrl { get; set; }
    public string Attribution { get; set; } = "&copy; <a href=\"https://www.openstreetmap.org/copyright\" target=\"_blank\">OpenStreetMap</a> contributors, &copy; <a href=\"https://openfreemap.org\" target=\"_blank\">OpenFreeMap</a>";
    public double DefaultLatitude { get; set; } = 25.2048; // Default center (Dubai / Middle East hub or configurable)
    public double DefaultLongitude { get; set; } = 55.2708;
    public double DefaultZoom { get; set; } = 12.0;
    public double MinimumZoom { get; set; } = 2.0;
    public double MaximumZoom { get; set; } = 19.0;
}

public sealed class DemoFleetSettings
{
    public const string SectionName = "DemoFleet";

    public bool Enabled { get; set; } = true;
    public int VehicleCount { get; set; } = 10;
    public int UpdateIntervalSeconds { get; set; } = 5;
    public bool AutoStart { get; set; } = false;
    public double DefaultCenterLatitude { get; set; } = 25.2048;
    public double DefaultCenterLongitude { get; set; } = 55.2708;
}

public sealed record MapClientConfigurationDto(
    string Provider,
    string StyleUrl,
    string? TileUrl,
    string Attribution,
    double DefaultLatitude,
    double DefaultLongitude,
    double DefaultZoom,
    double MinimumZoom,
    double MaximumZoom,
    bool IsDemoModeEnabled);

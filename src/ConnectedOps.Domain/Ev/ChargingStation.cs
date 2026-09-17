using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Ev;

public sealed class ChargingStation : BaseEntity
{
    private ChargingStation()
    {
    }

    public ChargingStation(
        Guid tenantId,
        string code,
        string name,
        ChargingStationType stationType,
        ChargingConnectorType connectorType,
        decimal maxPowerKw,
        int totalPlugs,
        string? address = null,
        double? latitude = null,
        double? longitude = null,
        decimal offPeakRatePerKwh = 0.15m,
        decimal peakRatePerKwh = 0.35m,
        string currency = "USD")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (maxPowerKw <= 0)
            throw new ArgumentException("MaxPowerKw must be positive.", nameof(maxPowerKw));
        if (totalPlugs <= 0)
            throw new ArgumentException("TotalPlugs must be positive.", nameof(totalPlugs));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        StationType = stationType;
        ConnectorType = connectorType;
        MaxPowerKw = maxPowerKw;
        TotalPlugs = totalPlugs;
        AvailablePlugs = totalPlugs;
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        OffPeakRatePerKwh = Math.Max(0m, offPeakRatePerKwh);
        PeakRatePerKwh = Math.Max(0m, peakRatePerKwh);
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ChargingStationType StationType { get; private set; }
    public ChargingConnectorType ConnectorType { get; private set; }
    public decimal MaxPowerKw { get; private set; }
    public int TotalPlugs { get; private set; }
    public int AvailablePlugs { get; private set; }
    public string? Address { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public decimal OffPeakRatePerKwh { get; private set; }
    public decimal PeakRatePerKwh { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; }

    public void UpdateOccupancy(int activeSessions)
    {
        AvailablePlugs = Math.Clamp(TotalPlugs - activeSessions, 0, TotalPlugs);
        MarkUpdated();
    }

    public void UpdateDetails(string name, ChargingConnectorType connectorType, decimal maxPowerKw, decimal offPeakRate, decimal peakRate)
    {
        Name = name.Trim();
        ConnectorType = connectorType;
        MaxPowerKw = Math.Max(1m, maxPowerKw);
        OffPeakRatePerKwh = Math.Max(0m, offPeakRate);
        PeakRatePerKwh = Math.Max(0m, peakRate);
        MarkUpdated();
    }
}

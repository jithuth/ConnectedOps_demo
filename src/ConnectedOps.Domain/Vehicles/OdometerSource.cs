namespace ConnectedOps.Domain.Vehicles;

public enum OdometerSource
{
    Manual = 1,
    Imported = 2,
    Telematics = 3,
    Maintenance = 4,
    Fuel = 5,
    MobileApp = 6,
    Other = 99
}

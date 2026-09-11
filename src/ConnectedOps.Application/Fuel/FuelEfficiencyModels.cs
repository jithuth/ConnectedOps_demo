using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Fuel;

public static class FuelUnitConverter
{
    public const decimal KmPerMile = 1.609344m;
    public const decimal LitersPerUsGallon = 3.785411784m;
    public const decimal LitersPerImperialGallon = 4.54609m;

    public static decimal ToKilometers(decimal value, OdometerUnit unit)
    {
        return unit switch
        {
            OdometerUnit.Miles => Math.Round(value * KmPerMile, 3),
            _ => value
        };
    }

    public static decimal FromKilometers(decimal kilometers, OdometerUnit targetUnit)
    {
        return targetUnit switch
        {
            OdometerUnit.Miles => Math.Round(kilometers / KmPerMile, 3),
            _ => kilometers
        };
    }

    public static decimal ToLiters(decimal quantity, FuelUnit unit)
    {
        return unit switch
        {
            FuelUnit.GallonUS => Math.Round(quantity * LitersPerUsGallon, 4),
            FuelUnit.GallonImperial => Math.Round(quantity * LitersPerImperialGallon, 4),
            _ => quantity
        };
    }

    public static decimal FromLiters(decimal liters, FuelUnit targetUnit)
    {
        return targetUnit switch
        {
            FuelUnit.GallonUS => Math.Round(liters / LitersPerUsGallon, 4),
            FuelUnit.GallonImperial => Math.Round(liters / LitersPerImperialGallon, 4),
            _ => liters
        };
    }

    public static decimal? CalculateKilometersPerLiter(decimal distanceKm, decimal volumeLiters)
    {
        if (distanceKm <= 0 || volumeLiters <= 0) return null;
        return Math.Round(distanceKm / volumeLiters, 3);
    }

    public static decimal? CalculateLitersPer100Km(decimal distanceKm, decimal volumeLiters)
    {
        if (distanceKm <= 0 || volumeLiters <= 0) return null;
        return Math.Round((volumeLiters / distanceKm) * 100m, 3);
    }

    public static decimal? CalculateMilesPerGallonUs(decimal distanceKm, decimal volumeLiters)
    {
        if (distanceKm <= 0 || volumeLiters <= 0) return null;
        var miles = distanceKm / KmPerMile;
        var usGallons = volumeLiters / LitersPerUsGallon;
        return Math.Round(miles / usGallons, 3);
    }

    public static decimal? CalculateMilesPerGallonUk(decimal distanceKm, decimal volumeLiters)
    {
        if (distanceKm <= 0 || volumeLiters <= 0) return null;
        var miles = distanceKm / KmPerMile;
        var ukGallons = volumeLiters / LitersPerImperialGallon;
        return Math.Round(miles / ukGallons, 3);
    }

    public static decimal? CalculateCostPerKilometer(decimal totalCost, decimal distanceKm)
    {
        if (distanceKm <= 0) return null;
        return Math.Round(totalCost / distanceKm, 4);
    }

    public static decimal? CalculateCostPerMile(decimal totalCost, decimal distanceKm)
    {
        if (distanceKm <= 0) return null;
        var miles = distanceKm / KmPerMile;
        return Math.Round(totalCost / miles, 4);
    }
}

public sealed record FuelEfficiencyDto(
    bool HasSufficientData,
    decimal? DistanceKilometers,
    decimal? DistanceMiles,
    decimal? VolumeLiters,
    decimal? KilometersPerLiter,
    decimal? LitersPer100Km,
    decimal? MilesPerGallonUS,
    decimal? MilesPerGallonUK,
    decimal? CostPerKilometer,
    decimal? CostPerMile,
    string? Notes);

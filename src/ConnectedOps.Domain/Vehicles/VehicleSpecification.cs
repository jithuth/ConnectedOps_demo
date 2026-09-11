using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleSpecification : BaseEntity
{
    private VehicleSpecification()
    {
    }

    public VehicleSpecification(
        Guid tenantId,
        Guid vehicleId,
        int? engineCapacityCc = null,
        decimal? enginePowerKw = null,
        int? cylinderCount = null,
        decimal? fuelTankCapacity = null,
        decimal? batteryVoltage = null,
        decimal? lengthMm = null,
        decimal? widthMm = null,
        decimal? heightMm = null,
        decimal? grossVehicleWeightKg = null,
        decimal? kerbWeightKg = null,
        decimal? payloadCapacityKg = null,
        int? axleCount = null,
        int? wheelCount = null,
        int? seatCount = null,
        string? bodyType = null,
        DriveType driveType = DriveType.FWD,
        string? emissionStandard = null,
        string? tyreSizeFront = null,
        string? tyreSizeRear = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        VehicleId = vehicleId;

        Update(
            engineCapacityCc,
            enginePowerKw,
            cylinderCount,
            fuelTankCapacity,
            batteryVoltage,
            lengthMm,
            widthMm,
            heightMm,
            grossVehicleWeightKg,
            kerbWeightKg,
            payloadCapacityKg,
            axleCount,
            wheelCount,
            seatCount,
            bodyType,
            driveType,
            emissionStandard,
            tyreSizeFront,
            tyreSizeRear);
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public int? EngineCapacityCc { get; private set; }
    public decimal? EnginePowerKw { get; private set; }
    public int? CylinderCount { get; private set; }
    public decimal? FuelTankCapacity { get; private set; }
    public decimal? BatteryVoltage { get; private set; }

    public decimal? LengthMm { get; private set; }
    public decimal? WidthMm { get; private set; }
    public decimal? HeightMm { get; private set; }

    public decimal? GrossVehicleWeightKg { get; private set; }
    public decimal? KerbWeightKg { get; private set; }
    public decimal? PayloadCapacityKg { get; private set; }

    public int? AxleCount { get; private set; }
    public int? WheelCount { get; private set; }
    public int? SeatCount { get; private set; }

    public string? BodyType { get; private set; }
    public DriveType DriveType { get; private set; }
    public string? EmissionStandard { get; private set; }

    public string? TyreSizeFront { get; private set; }
    public string? TyreSizeRear { get; private set; }

    public void Update(
        int? engineCapacityCc,
        decimal? enginePowerKw,
        int? cylinderCount,
        decimal? fuelTankCapacity,
        decimal? batteryVoltage,
        decimal? lengthMm,
        decimal? widthMm,
        decimal? heightMm,
        decimal? grossVehicleWeightKg,
        decimal? kerbWeightKg,
        decimal? payloadCapacityKg,
        int? axleCount,
        int? wheelCount,
        int? seatCount,
        string? bodyType,
        DriveType driveType,
        string? emissionStandard,
        string? tyreSizeFront,
        string? tyreSizeRear)
    {
        EngineCapacityCc = engineCapacityCc;
        EnginePowerKw = enginePowerKw;
        CylinderCount = cylinderCount;
        FuelTankCapacity = fuelTankCapacity;
        BatteryVoltage = batteryVoltage;

        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;

        GrossVehicleWeightKg = grossVehicleWeightKg;
        KerbWeightKg = kerbWeightKg;
        PayloadCapacityKg = payloadCapacityKg;

        AxleCount = axleCount;
        WheelCount = wheelCount;
        SeatCount = seatCount;

        BodyType = bodyType?.Trim();
        DriveType = driveType;
        EmissionStandard = emissionStandard?.Trim();

        TyreSizeFront = tyreSizeFront?.Trim();
        TyreSizeRear = tyreSizeRear?.Trim();

        MarkUpdated();
    }
}

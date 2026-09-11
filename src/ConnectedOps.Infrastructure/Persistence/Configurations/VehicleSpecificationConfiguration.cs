using ConnectedOps.Domain.Vehicles;
using DriveType = ConnectedOps.Domain.Vehicles.DriveType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleSpecificationConfiguration : IEntityTypeConfiguration<VehicleSpecification>
{
    public void Configure(EntityTypeBuilder<VehicleSpecification> builder)
    {
        builder.ToTable("VehicleSpecifications");

        builder.HasKey(x => x.VehicleId);

        builder.Property(x => x.VehicleId)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.EnginePowerKw)
            .HasPrecision(18, 2);

        builder.Property(x => x.FuelTankCapacity)
            .HasPrecision(18, 2);

        builder.Property(x => x.BatteryVoltage)
            .HasPrecision(18, 2);

        builder.Property(x => x.LengthMm)
            .HasPrecision(18, 2);

        builder.Property(x => x.WidthMm)
            .HasPrecision(18, 2);

        builder.Property(x => x.HeightMm)
            .HasPrecision(18, 2);

        builder.Property(x => x.GrossVehicleWeightKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.KerbWeightKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.PayloadCapacityKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.BodyType)
            .HasMaxLength(100);

        builder.Property(x => x.DriveType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EmissionStandard)
            .HasMaxLength(50);

        builder.Property(x => x.TyreSizeFront)
            .HasMaxLength(50);

        builder.Property(x => x.TyreSizeRear)
            .HasMaxLength(50);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

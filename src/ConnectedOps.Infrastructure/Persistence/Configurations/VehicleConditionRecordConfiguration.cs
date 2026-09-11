using ConnectedOps.Domain.FleetOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleConditionRecordConfiguration : IEntityTypeConfiguration<VehicleConditionRecord>
{
    public void Configure(EntityTypeBuilder<VehicleConditionRecord> builder)
    {
        builder.ToTable("VehicleConditionRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.Condition)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Odometer)
            .HasPrecision(18, 2);

        builder.Property(x => x.RecordedAtUtc)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UsageSession)
            .WithMany()
            .HasForeignKey(x => x.UsageSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
    }
}

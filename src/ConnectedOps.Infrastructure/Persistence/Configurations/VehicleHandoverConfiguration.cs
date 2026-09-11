using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleHandoverConfiguration : IEntityTypeConfiguration<VehicleHandover>
{
    public void Configure(EntityTypeBuilder<VehicleHandover> builder)
    {
        builder.ToTable("VehicleHandovers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.ToDriverId)
            .IsRequired();

        builder.Property(x => x.HandoverAtUtc)
            .IsRequired();

        builder.Property(x => x.Odometer)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.OdometerUnit)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Condition)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.AcknowledgedByFromDriver)
            .IsRequired();

        builder.Property(x => x.AcknowledgedByToDriver)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.HandoverAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.ToDriverId });
        builder.HasIndex(x => new { x.TenantId, x.FromDriverId });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromDriver)
            .WithMany()
            .HasForeignKey(x => x.FromDriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToDriver)
            .WithMany()
            .HasForeignKey(x => x.ToDriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromUsageSession)
            .WithMany()
            .HasForeignKey(x => x.FromUsageSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToUsageSession)
            .WithMany()
            .HasForeignKey(x => x.ToUsageSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

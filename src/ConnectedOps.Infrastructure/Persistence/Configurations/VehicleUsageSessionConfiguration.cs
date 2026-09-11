using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleUsageSessionConfiguration : IEntityTypeConfiguration<VehicleUsageSession>
{
    public void Configure(EntityTypeBuilder<VehicleUsageSession> builder)
    {
        builder.ToTable("VehicleUsageSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.CheckedOutAtUtc)
            .IsRequired();

        builder.Property(x => x.StartOdometer)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.EndOdometer)
            .HasPrecision(18, 2);

        builder.Property(x => x.OdometerUnit)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CheckoutCondition)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CheckInCondition)
            .HasConversion<int>();

        builder.Property(x => x.Purpose)
            .HasMaxLength(250);

        builder.Property(x => x.Reference)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Filtered unique index: at most ONE Open session per vehicle per tenant
        builder.HasIndex(x => new { x.TenantId, x.VehicleId })
            .HasFilter("[Status] = 1")
            .IsUnique();

        // Filtered unique index: at most ONE Open session per driver per tenant
        builder.HasIndex(x => new { x.TenantId, x.DriverId })
            .HasFilter("[Status] = 1")
            .IsUnique();

        // Additional query indexes
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.CheckedOutAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.CheckedInAtUtc });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DriverVehicleAssignment)
            .WithMany()
            .HasForeignKey(x => x.DriverVehicleAssignmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FleetShiftAssignment)
            .WithMany()
            .HasForeignKey(x => x.FleetShiftAssignmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

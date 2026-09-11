using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class FleetShiftAssignmentConfiguration : IEntityTypeConfiguration<FleetShiftAssignment>
{
    public void Configure(EntityTypeBuilder<FleetShiftAssignment> builder)
    {
        builder.ToTable("FleetShiftAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.FleetShiftId)
            .IsRequired();

        builder.Property(x => x.AssignmentDate)
            .IsRequired();

        builder.Property(x => x.StartDateTimeUtc)
            .IsRequired();

        builder.Property(x => x.AssignmentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.FleetShiftId, x.AssignmentDate });
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.StartDateTimeUtc });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.StartDateTimeUtc });
        builder.HasIndex(x => new { x.TenantId, x.AssignmentStatus });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FleetShift)
            .WithMany()
            .HasForeignKey(x => x.FleetShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using ConnectedOps.Domain.Dispatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DispatchJobConfiguration : IEntityTypeConfiguration<DispatchJob>
{
    public void Configure(EntityTypeBuilder<DispatchJob> builder)
    {
        builder.ToTable("DispatchJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CustomerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(x => x.CustomerEmail)
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SpecialInstructions)
            .HasMaxLength(1000);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.WeightKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.VolumeM3)
            .HasPrecision(18, 3);

        builder.HasIndex(x => new { x.TenantId, x.JobNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssignedRouteId });

        builder.HasOne(x => x.AssignedRoute)
            .WithMany()
            .HasForeignKey(x => x.AssignedRouteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AssignedVehicle)
            .WithMany()
            .HasForeignKey(x => x.AssignedVehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AssignedDriver)
            .WithMany()
            .HasForeignKey(x => x.AssignedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DispatchRouteConfiguration : IEntityTypeConfiguration<DispatchRoute>
{
    public void Configure(EntityTypeBuilder<DispatchRoute> builder)
    {
        builder.ToTable("DispatchRoutes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RouteNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.EstimatedDistanceKm)
            .HasPrecision(18, 2);

        builder.Property(x => x.ActualDistanceKm)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.RouteNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.ScheduledDate });
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.StartLocation)
            .WithMany()
            .HasForeignKey(x => x.StartLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EndLocation)
            .WithMany()
            .HasForeignKey(x => x.EndLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Stops)
            .WithOne(s => s.Route)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DispatchRouteStopConfiguration : IEntityTypeConfiguration<DispatchRouteStop>
{
    public void Configure(EntityTypeBuilder<DispatchRouteStop> builder)
    {
        builder.ToTable("DispatchRouteStops");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.EstimatedDistanceKm)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.RouteId, x.SequenceOrder });
        builder.HasIndex(x => new { x.TenantId, x.JobId });

        builder.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProofOfDelivery)
            .WithOne(p => p.RouteStop)
            .HasForeignKey<ProofOfDelivery>(p => p.RouteStopId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ProofOfDeliveryConfiguration : IEntityTypeConfiguration<ProofOfDelivery>
{
    public void Configure(EntityTypeBuilder<ProofOfDelivery> builder)
    {
        builder.ToTable("ProofOfDeliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.JobId });
        builder.HasIndex(x => new { x.TenantId, x.RouteStopId });

        builder.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using ConnectedOps.Domain.Predictive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleSubsystemHealthConfiguration : IEntityTypeConfiguration<VehicleSubsystemHealth>
{
    public void Configure(EntityTypeBuilder<VehicleSubsystemHealth> builder)
    {
        builder.ToTable("VehicleSubsystemHealths");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.HealthScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.DiagnosticsNotes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.Subsystem })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.HealthScore });
    }
}

public sealed class PredictiveMaintenanceAlertConfiguration : IEntityTypeConfiguration<PredictiveMaintenanceAlert>
{
    public void Configure(EntityTypeBuilder<PredictiveMaintenanceAlert> builder)
    {
        builder.ToTable("PredictiveMaintenanceAlerts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SymptomDescription)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.RecommendedAction)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.FailureProbability)
            .HasPrecision(5, 2);

        builder.Property(x => x.EstimatedRepairCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.DismissReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.RiskLevel });
        builder.HasIndex(x => x.DetectedAtUtc);
    }
}

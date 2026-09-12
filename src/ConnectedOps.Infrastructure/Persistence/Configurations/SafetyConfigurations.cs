using ConnectedOps.Domain.Safety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class SafetyIncidentConfiguration : IEntityTypeConfiguration<SafetyIncident>
{
    public void Configure(EntityTypeBuilder<SafetyIncident> builder)
    {
        builder.ToTable("SafetyIncidents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IncidentNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ImmediateActionTaken).HasMaxLength(2000);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Investigation)
            .WithOne(x => x.SafetyIncident)
            .HasForeignKey<SafetyIncidentInvestigation>(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.IncidentNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.Severity });
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
    }
}

public sealed class SafetyIncidentParticipantConfiguration : IEntityTypeConfiguration<SafetyIncidentParticipant>
{
    public void Configure(EntityTypeBuilder<SafetyIncidentParticipant> builder)
    {
        builder.ToTable("SafetyIncidentParticipants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
    }
}

public sealed class SafetyIncidentVehicleConfiguration : IEntityTypeConfiguration<SafetyIncidentVehicle>
{
    public void Configure(EntityTypeBuilder<SafetyIncidentVehicle> builder)
    {
        builder.ToTable("SafetyIncidentVehicles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DamageDescription).HasMaxLength(1000);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
    }
}

public sealed class SafetyIncidentAssetConfiguration : IEntityTypeConfiguration<SafetyIncidentAsset>
{
    public void Configure(EntityTypeBuilder<SafetyIncidentAsset> builder)
    {
        builder.ToTable("SafetyIncidentAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DamageDescription).HasMaxLength(1000);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.Assets)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

public sealed class SafetyIncidentEvidenceConfiguration : IEntityTypeConfiguration<SafetyIncidentEvidence>
{
    public void Configure(EntityTypeBuilder<SafetyIncidentEvidence> builder)
    {
        builder.ToTable("SafetyIncidentEvidence");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        builder.Property(x => x.FileObjectKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.Evidence)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
    }
}

public sealed class SafetyIncidentInvestigationConfiguration : IEntityTypeConfiguration<SafetyIncidentInvestigation>
{
    public void Configure(EntityTypeBuilder<SafetyIncidentInvestigation> builder)
    {
        builder.ToTable("SafetyIncidentInvestigations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.Property(x => x.RootCauseDescription).HasMaxLength(2000);
        builder.Property(x => x.ContributingFactors).HasMaxLength(2000);
        builder.Property(x => x.Recommendation).HasMaxLength(2000);

        builder.HasOne(x => x.InvestigatorEmployee)
            .WithMany()
            .HasForeignKey(x => x.InvestigatorEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId }).IsUnique();
    }
}

public sealed class SafetyViolationConfiguration : IEntityTypeConfiguration<SafetyViolation>
{
    public void Configure(EntityTypeBuilder<SafetyViolation> builder)
    {
        builder.ToTable("SafetyViolations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.Violations)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.IsResolved, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
    }
}

public sealed class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> builder)
    {
        builder.ToTable("CorrectiveActions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(2000);

        builder.HasOne(x => x.AssignedEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SafetyIncident)
            .WithMany(x => x.CorrectiveActions)
            .HasForeignKey(x => x.SafetyIncidentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SafetyViolation)
            .WithMany()
            .HasForeignKey(x => x.SafetyViolationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ComplianceRecord)
            .WithMany()
            .HasForeignKey(x => x.ComplianceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.Status, x.DueDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.AssignedEmployeeId });
        builder.HasIndex(x => new { x.TenantId, x.SafetyIncidentId });
    }
}

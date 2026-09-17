using ConnectedOps.Domain.Alerts;
using ConnectedOps.Domain.ColdChain;
using ConnectedOps.Domain.Inspections;
using ConnectedOps.Domain.TollsAndFines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsEnabled });

        builder.HasMany(x => x.Conditions)
            .WithOne(c => c.AlertRule)
            .HasForeignKey(c => c.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AlertRuleConditionConfiguration : IEntityTypeConfiguration<AlertRuleCondition>
{
    public void Configure(EntityTypeBuilder<AlertRuleCondition> builder)
    {
        builder.ToTable("AlertRuleConditions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ThresholdValue).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => new { x.TenantId, x.AlertRuleId });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.Severity });
        builder.HasIndex(x => new { x.TenantId, x.TriggeredAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });

        builder.HasOne(x => x.AlertRule)
            .WithMany()
            .HasForeignKey(x => x.AlertRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("NotificationMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recipient).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ExternalMessageId).HasMaxLength(150);

        builder.HasIndex(x => new { x.TenantId, x.SentAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.Channel });

        builder.HasOne(x => x.Alert)
            .WithMany()
            .HasForeignKey(x => x.AlertId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DvirInspectionConfiguration : IEntityTypeConfiguration<DvirInspection>
{
    public void Configure(EntityTypeBuilder<DvirInspection> builder)
    {
        builder.ToTable("DvirInspections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InspectionNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.LocationName).HasMaxLength(200);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.MechanicName).HasMaxLength(150);
        builder.Property(x => x.MechanicNotes).HasMaxLength(1000);
        builder.Property(x => x.Odometer).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.InspectionNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.DvirInspection)
            .HasForeignKey(i => i.DvirInspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DvirItemCheckConfiguration : IEntityTypeConfiguration<DvirItemCheck>
{
    public void Configure(EntityTypeBuilder<DvirItemCheck> builder)
    {
        builder.ToTable("DvirItemChecks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ItemName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.DefectDescription).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.DvirInspectionId });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class TollTransactionConfiguration : IEntityTypeConfiguration<TollTransaction>
{
    public void Configure(EntityTypeBuilder<TollTransaction> builder)
    {
        builder.ToTable("TollTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TollGateName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.TollGateCode).HasMaxLength(50);
        builder.Property(x => x.TagNumber).HasMaxLength(100);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.TransactionTimeUtc });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.MatchedDriverId });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MatchedDriver)
            .WithMany()
            .HasForeignKey(x => x.MatchedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class TrafficViolationConfiguration : IEntityTypeConfiguration<TrafficViolation>
{
    public void Configure(EntityTypeBuilder<TrafficViolation> builder)
    {
        builder.ToTable("TrafficViolations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.AuthorityName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ViolationCode).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Location).HasMaxLength(250);
        builder.Property(x => x.DisputeReason).HasMaxLength(1000);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.Property(x => x.FineAmount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.TicketNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.MatchedDriverId });
        builder.HasIndex(x => new { x.TenantId, x.LiabilityStatus });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MatchedDriver)
            .WithMany()
            .HasForeignKey(x => x.MatchedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class CargoSensorDeviceConfiguration : IEntityTypeConfiguration<CargoSensorDevice>
{
    public void Configure(EntityTypeBuilder<CargoSensorDevice> builder)
    {
        builder.ToTable("CargoSensorDevices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SensorTagNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CompartmentName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MacAddress).HasMaxLength(50);

        builder.HasIndex(x => new { x.TenantId, x.SensorTagNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class CargoTelemetryReadingConfiguration : IEntityTypeConfiguration<CargoTelemetryReading>
{
    public void Configure(EntityTypeBuilder<CargoTelemetryReading> builder)
    {
        builder.ToTable("CargoTelemetryReadings");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TenantId, x.CargoSensorDeviceId, x.RecordedAtUtc });

        builder.HasOne(x => x.CargoSensorDevice)
            .WithMany()
            .HasForeignKey(x => x.CargoSensorDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ColdChainExcursionConfiguration : IEntityTypeConfiguration<ColdChainExcursion>
{
    public void Configure(EntityTypeBuilder<ColdChainExcursion> builder)
    {
        builder.ToTable("ColdChainExcursions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompartmentName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LocationName).HasMaxLength(200);
        builder.Property(x => x.ActionTaken).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.StartedAtUtc });

        builder.HasOne(x => x.CargoSensorDevice)
            .WithMany()
            .HasForeignKey(x => x.CargoSensorDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

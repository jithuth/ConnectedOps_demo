using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleDowntimeRecordConfiguration : IEntityTypeConfiguration<VehicleDowntimeRecord>
{
    public void Configure(EntityTypeBuilder<VehicleDowntimeRecord> builder)
    {
        builder.ToTable("VehicleDowntimeRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.DowntimeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenanceRecord)
            .WithMany(x => x.DowntimeRecords)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

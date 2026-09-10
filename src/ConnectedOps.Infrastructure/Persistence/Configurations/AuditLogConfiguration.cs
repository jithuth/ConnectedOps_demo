using ConnectedOps.Domain.Auditing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.RequestPath)
            .HasMaxLength(1000);

        builder.Property(x => x.TraceId)
            .HasMaxLength(100);

        builder.Property(x => x.TenantId)
            .IsRequired(false);

        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x =>
            new
            {
                x.TenantId,
                x.CreatedAtUtc
            });

        builder.HasIndex(x =>
            new
            {
                x.TenantId,
                x.EntityType,
                x.EntityId
            });

        builder.HasIndex(x =>
            new
            {
                x.TenantId,
                x.UserId,
                x.CreatedAtUtc
            });

        // Audit history should never disappear because another
        // entity was soft-deleted.
        //
        // Intentionally do NOT add an IsDeleted query filter here.
    }
}
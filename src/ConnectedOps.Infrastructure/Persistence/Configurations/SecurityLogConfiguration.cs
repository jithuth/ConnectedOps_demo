using ConnectedOps.Domain.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class SecurityLogConfiguration
    : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(
        EntityTypeBuilder<SecurityLog> builder)
    {
        builder.ToTable("SecurityLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Succeeded)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.RequestPath)
            .HasMaxLength(1000);

        builder.Property(x => x.TraceId)
            .HasMaxLength(100);

        builder.Property(x => x.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.Email);

        builder.HasIndex(x =>
            new
            {
                x.EventType,
                x.CreatedAtUtc
            });

        builder.HasIndex(x =>
            new
            {
                x.TenantId,
                x.CreatedAtUtc
            });

        builder.HasIndex(x =>
            new
            {
                x.IpAddress,
                x.CreatedAtUtc
            });

        // Security logs should not disappear through soft-delete filtering.
    }
}
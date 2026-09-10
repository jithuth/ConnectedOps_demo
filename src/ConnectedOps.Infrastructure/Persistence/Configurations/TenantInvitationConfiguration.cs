using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TenantInvitationConfiguration
    : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(
        EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.ToTable("TenantInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Email
        });

        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TenantRole)
            .WithMany()
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(
            x => !x.IsDeleted);
    }
}
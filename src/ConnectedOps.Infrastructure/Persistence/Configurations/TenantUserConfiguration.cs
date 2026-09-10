using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TenantUserConfiguration
    : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(
        EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.IsDefaultTenant)
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId
        })
        .IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
using ConnectedOps.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TenantUserRoleConfiguration
    : IEntityTypeConfiguration<TenantUserRole>
{
    public void Configure(
        EntityTypeBuilder<TenantUserRole> builder)
    {
        builder.ToTable("TenantUserRoles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.TenantUserId,
            x.TenantRoleId
        })
        .IsUnique();

        builder.HasOne(x => x.TenantUser)
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TenantRole)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
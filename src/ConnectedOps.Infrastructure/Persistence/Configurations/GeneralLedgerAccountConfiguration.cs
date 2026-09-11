using ConnectedOps.Domain.Accounting;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class GeneralLedgerAccountConfiguration : IEntityTypeConfiguration<GeneralLedgerAccount>
{
    public void Configure(EntityTypeBuilder<GeneralLedgerAccount> builder)
    {
        builder.ToTable("GeneralLedgerAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AccountName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.AccountCode })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

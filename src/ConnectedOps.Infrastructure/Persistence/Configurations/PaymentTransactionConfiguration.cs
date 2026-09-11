using ConnectedOps.Domain.Billing;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionReference)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.GatewayTransactionId)
            .HasMaxLength(150);

        builder.Property(x => x.GatewayResponseCode)
            .HasMaxLength(50);

        builder.Property(x => x.GatewayResponseMessage)
            .HasMaxLength(500);

        builder.Property(x => x.PayerEmail)
            .HasMaxLength(256);

        builder.Property(x => x.LastFourDigits)
            .HasMaxLength(4);

        builder.Property(x => x.CardBrand)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.TransactionReference })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

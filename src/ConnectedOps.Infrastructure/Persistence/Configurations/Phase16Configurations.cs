using ConnectedOps.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.IsEnabled });

        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.SecretKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SubscribedEvents).HasMaxLength(1000).IsRequired();
    }
}

public sealed class WebhookDeliveryAttemptConfiguration : IEntityTypeConfiguration<WebhookDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryAttempt> builder)
    {
        builder.ToTable("WebhookDeliveryAttempts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.SubscriptionId, x.AttemptedAtUtc });

        builder.HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
    }
}

public sealed class TenantApiKeyConfiguration : IEntityTypeConfiguration<TenantApiKey>
{
    public void Configure(EntityTypeBuilder<TenantApiKey> builder)
    {
        builder.ToTable("TenantApiKeys");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.KeyPrefix });
        builder.HasIndex(x => x.KeyHash).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(24).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(500).IsRequired();
    }
}

public sealed class ErpExportBatchConfiguration : IEntityTypeConfiguration<ErpExportBatch>
{
    public void Configure(EntityTypeBuilder<ErpExportBatch> builder)
    {
        builder.ToTable("ErpExportBatches");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.BatchNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.TargetSystem, x.Status });

        builder.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExternalReference).HasMaxLength(150);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
    }
}

public sealed class FuelFeedSyncLogConfiguration : IEntityTypeConfiguration<FuelFeedSyncLog>
{
    public void Configure(EntityTypeBuilder<FuelFeedSyncLog> builder)
    {
        builder.ToTable("FuelFeedSyncLogs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Provider, x.SyncedAtUtc });

        builder.Property(x => x.TotalSpend).HasPrecision(18, 2);
        builder.Property(x => x.TotalLiters).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.RawSummary).HasMaxLength(2000);
    }
}

using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantSettingsEntity = ConnectedOps.Domain.Tenancy.TenantSettings;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TenantSettingsConfiguration
    : IEntityTypeConfiguration<TenantSettingsEntity>
{
    public void Configure(
    EntityTypeBuilder<TenantSettingsEntity> builder)
    {
        builder.ToTable("TenantSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TimeZone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.DistanceUnit)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.FuelUnit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DateFormat)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TimeFormat)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.WeekStartsOn)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.EnableNotifications)
            .IsRequired();

        builder.Property(x => x.EnableEmailNotifications)
            .IsRequired();

        builder.Property(x => x.EnableSmsNotifications)
            .IsRequired();

        builder.Property(x => x.EnablePushNotifications)
            .IsRequired();

        builder.HasIndex(x => x.TenantId)
            .IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithOne(x => x.Settings)
            .HasForeignKey<TenantSettingsEntity>(
                x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
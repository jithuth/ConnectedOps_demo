using ConnectedOps.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceExpenseConfiguration : IEntityTypeConfiguration<VehicleMaintenanceExpense>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceExpense> builder)
    {
        builder.ToTable("VehicleMaintenanceExpenses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenanceRecordId)
            .IsRequired();

        builder.Property(x => x.ExpenseType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(10);

        builder.Property(x => x.Reference)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

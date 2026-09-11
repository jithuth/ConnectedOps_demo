using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Billing;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public SubscriptionPlanService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SubscriptionPlanDto>> GetPublicPlansAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsPublic)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubscriptionPlanDto>> GetAllPlansAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return plan is null ? null : MapToDto(plan);
    }

    public async Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _dbContext.SubscriptionPlans.AnyAsync(x => x.Code == code, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Plan with code '{code}' already exists.");

        var plan = new SubscriptionPlan(
            request.Name,
            code,
            request.Price,
            request.BillingInterval,
            request.Currency,
            request.Description,
            request.MaxVehicles,
            request.MaxAssets,
            request.MaxUsers,
            request.MaxStorageGb,
            request.HasAdvancedAnalytics,
            request.HasApiAccess,
            request.HasCustomBranding,
            request.HasAuditExport,
            request.IsPublic,
            request.SortOrder);

        _dbContext.SubscriptionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(plan);
    }

    public async Task<SubscriptionPlanDto> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException($"Subscription plan with ID '{id}' was not found.");

        plan.UpdateDetails(
            request.Name,
            request.Description,
            request.Price,
            request.BillingInterval,
            request.Currency,
            request.MaxVehicles,
            request.MaxAssets,
            request.MaxUsers,
            request.MaxStorageGb,
            request.HasAdvancedAnalytics,
            request.HasApiAccess,
            request.HasCustomBranding,
            request.HasAuditExport,
            request.IsPublic,
            request.SortOrder);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(plan);
    }

    public async Task ActivatePlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException($"Subscription plan with ID '{id}' was not found.");

        plan.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivatePlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException($"Subscription plan with ID '{id}' was not found.");

        plan.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SubscriptionPlanDto MapToDto(SubscriptionPlan x) =>
        new(
            x.Id,
            x.Name,
            x.Code,
            x.Price,
            x.Currency,
            x.BillingInterval,
            x.Description,
            x.MaxVehicles,
            x.MaxAssets,
            x.MaxUsers,
            x.MaxStorageGb,
            x.HasAdvancedAnalytics,
            x.HasApiAccess,
            x.HasCustomBranding,
            x.HasAuditExport,
            x.IsActive,
            x.IsPublic,
            x.SortOrder,
            x.CreatedAtUtc);
}

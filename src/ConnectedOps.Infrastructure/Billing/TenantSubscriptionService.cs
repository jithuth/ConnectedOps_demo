using ConnectedOps.Application.Billing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Billing;

public sealed class TenantSubscriptionService : ITenantSubscriptionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantSubscriptionService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<TenantSubscriptionDto?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (sub is null)
            return null;

        var vehicleCount = 0; // Vehicles module count placeholder or count of assets
        var assetCount = 0;
        var userCount = await _dbContext.TenantUsers
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        return MapToDto(sub, vehicleCount, assetCount, userCount);
    }

    public async Task<TenantSubscriptionDto> ChangePlanAsync(ChangeSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var newPlan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(x => x.Id == request.NewPlanId && x.IsActive, cancellationToken);

        if (newPlan is null)
            throw new KeyNotFoundException("Selected subscription plan does not exist or is inactive.");

        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        var nextPeriodEnd = DateTime.UtcNow.AddMonths(newPlan.BillingInterval == BillingInterval.Yearly ? 12 : 1);

        if (sub is null)
        {
            sub = new TenantSubscription(
                tenantId,
                newPlan.Id,
                DateTime.UtcNow,
                nextPeriodEnd);

            _dbContext.TenantSubscriptions.Add(sub);
        }
        else
        {
            sub.ChangePlan(newPlan.Id, nextPeriodEnd);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userCount = await _dbContext.TenantUsers
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        return MapToDto(sub, 0, 0, userCount);
    }

    public async Task<TenantSubscriptionDto> CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (sub is null)
            throw new KeyNotFoundException("No active subscription found for this organization.");

        sub.Cancel(request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userCount = await _dbContext.TenantUsers
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        return MapToDto(sub, 0, 0, userCount);
    }

    public async Task<TenantSubscriptionDto> ReactivateSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (sub is null)
            throw new KeyNotFoundException("No subscription found for this organization.");

        sub.Reactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userCount = await _dbContext.TenantUsers
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        return MapToDto(sub, 0, 0, userCount);
    }

    public async Task<TenantSubscriptionDto> SetCustomQuotasAsync(SetCustomQuotasRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (sub is null)
            throw new KeyNotFoundException("No subscription found for this organization.");

        sub.SetCustomQuotas(request.CustomMaxVehicles, request.CustomMaxAssets, request.CustomMaxUsers);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userCount = await _dbContext.TenantUsers
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        return MapToDto(sub, 0, 0, userCount);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static TenantSubscriptionDto MapToDto(TenantSubscription sub, int vehicleCount, int assetCount, int userCount)
    {
        var effectiveVehicles = sub.CustomMaxVehicles ?? sub.Plan?.MaxVehicles ?? 50;
        var effectiveAssets = sub.CustomMaxAssets ?? sub.Plan?.MaxAssets ?? 100;
        var effectiveUsers = sub.CustomMaxUsers ?? sub.Plan?.MaxUsers ?? 10;

        return new(
            sub.Id,
            sub.TenantId,
            sub.PlanId,
            sub.Plan?.Name ?? "Custom Plan",
            sub.Plan?.Code ?? "CUSTOM",
            sub.Plan?.Price ?? 0m,
            sub.Plan?.Currency ?? "USD",
            sub.Plan?.BillingInterval ?? BillingInterval.Monthly,
            sub.Status,
            sub.CurrentPeriodStartUtc,
            sub.CurrentPeriodEndUtc,
            sub.TrialEndUtc,
            sub.AutoRenew,
            sub.IsInTrial,
            sub.CancelledAtUtc,
            sub.CancellationReason,
            effectiveVehicles,
            effectiveAssets,
            effectiveUsers,
            vehicleCount,
            assetCount,
            userCount,
            sub.CustomMaxVehicles,
            sub.CustomMaxAssets,
            sub.CustomMaxUsers);
    }
}

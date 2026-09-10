using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.TenantSettings;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.TenantSettings;

public sealed class TenantSettingsService
    : ITenantSettingsService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantSettingsService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<TenantSettingsResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var settings =
            await GetOrCreateSettingsAsync(
                tenantId,
                cancellationToken);

        return Map(settings);
    }

    public async Task<TenantSettingsResult>
        UpdateGeneralAsync(
            UpdateTenantGeneralSettingsRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = GetTenantId();

        var settings =
            await GetOrCreateSettingsAsync(
                tenantId,
                cancellationToken);

        settings.UpdateGeneralSettings(
            request.TimeZone,
            request.CurrencyCode,
            request.DistanceUnit,
            request.FuelUnit,
            request.DateFormat,
            request.TimeFormat,
            request.WeekStartsOn,
            request.LanguageCode);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Map(settings);
    }

    public async Task<TenantSettingsResult>
        UpdateNotificationsAsync(
            UpdateTenantNotificationSettingsRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = GetTenantId();

        var settings =
            await GetOrCreateSettingsAsync(
                tenantId,
                cancellationToken);

        settings.UpdateNotificationSettings(
            request.EnableNotifications,
            request.EnableEmailNotifications,
            request.EnableSmsNotifications,
            request.EnablePushNotifications);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Map(settings);
    }

    private async Task<
        Domain.Tenancy.TenantSettings>
        GetOrCreateSettingsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var settings =
            await _dbContext.TenantSettings
                .SingleOrDefaultAsync(
                    x => x.TenantId == tenantId,
                    cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        var tenantExists =
            await _dbContext.Tenants
                .AnyAsync(
                    x =>
                        x.Id == tenantId &&
                        x.IsActive,
                    cancellationToken);

        if (!tenantExists)
        {
            throw new InvalidOperationException(
                "Active tenant was not found.");
        }

        settings =
            new Domain.Tenancy.TenantSettings(
                tenantId);

        _dbContext.TenantSettings.Add(
            settings);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return settings;
    }

    private Guid GetTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        if (_currentUserContext.TenantId
            is not Guid tenantId)
        {
            throw new UnauthorizedAccessException(
                "Tenant ID is missing.");
        }

        return tenantId;
    }

    private static TenantSettingsResult Map(
        Domain.Tenancy.TenantSettings settings)
    {
        return new TenantSettingsResult(
            settings.TenantId,
            settings.TimeZone,
            settings.CurrencyCode,
            settings.DistanceUnit,
            settings.FuelUnit,
            settings.DateFormat,
            settings.TimeFormat,
            settings.WeekStartsOn,
            settings.LanguageCode,
            settings.EnableNotifications,
            settings.EnableEmailNotifications,
            settings.EnableSmsNotifications,
            settings.EnablePushNotifications);
    }
}
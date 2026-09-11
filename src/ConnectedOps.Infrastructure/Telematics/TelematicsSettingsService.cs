using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelematicsSettingsService : ITelematicsSettingsService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public TelematicsSettingsService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for telematics settings.");
    }

    public async Task<TelematicsSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var settings = await _dbContext.TelematicsSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings == null)
        {
            settings = new TelematicsSettings(tenantId);
            _dbContext.TelematicsSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(settings);
    }

    public async Task<TelematicsSettingsDto> UpdateSettingsAsync(
        UpdateTelematicsSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var settings = await _dbContext.TelematicsSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings == null)
        {
            settings = new TelematicsSettings(
                tenantId,
                request.OfflineThresholdMinutes,
                request.TelemetryRetentionDays,
                request.MaxAcceptedFutureMinutes,
                request.MaxAcceptedPastDays,
                request.OdometerUpdateThresholdKm,
                request.OdometerUpdateMinIntervalMinutes);

            _dbContext.TelematicsSettings.Add(settings);
        }
        else
        {
            settings.Update(
                request.OfflineThresholdMinutes,
                request.TelemetryRetentionDays,
                request.MaxAcceptedFutureMinutes,
                request.MaxAcceptedPastDays,
                request.OdometerUpdateThresholdKm,
                request.OdometerUpdateMinIntervalMinutes,
                _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TelematicsSettingsUpdated,
                "TelematicsSettings",
                settings.Id.ToString(),
                $"Updated telematics settings (Offline threshold: {settings.OfflineThresholdMinutes}m, Retention: {settings.TelemetryRetentionDays}d)."),
            cancellationToken);

        return MapToDto(settings);
    }

    private static TelematicsSettingsDto MapToDto(TelematicsSettings s) =>
        new(
            s.Id,
            s.TenantId,
            s.OfflineThresholdMinutes,
            s.TelemetryRetentionDays,
            s.MaxAcceptedFutureMinutes,
            s.MaxAcceptedPastDays,
            s.OdometerUpdateThresholdKm,
            s.OdometerUpdateMinIntervalMinutes,
            s.CreatedAtUtc,
            s.UpdatedAtUtc);
}

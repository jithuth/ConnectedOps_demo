using ConnectedOps.Application.Alerts;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Alerts;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Alerts;

public sealed class AlertService : IAlertService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        if (!_currentUserContext.TenantId.HasValue || _currentUserContext.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return _currentUserContext.TenantId.Value;
    }

    public async Task<AlertDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;

        var alerts = await _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .Where(a => a.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var activeAlerts = alerts
            .Where(a => a.Status == AlertStatus.Triggered || a.Status == AlertStatus.Acknowledged)
            .ToList();

        var notifications = await _dbContext.NotificationMessages
            .Where(n => n.TenantId == tenantId && n.SentAtUtc >= today)
            .ToListAsync(cancellationToken);

        return new AlertDashboardDto(
            TotalActiveAlerts: activeAlerts.Count,
            CriticalAlertsCount: activeAlerts.Count(a => a.Severity == AlertSeverity.Critical || a.Severity == AlertSeverity.Urgent),
            WarningAlertsCount: activeAlerts.Count(a => a.Severity == AlertSeverity.Warning),
            ResolvedTodayCount: alerts.Count(a => a.Status == AlertStatus.Resolved && a.ResolvedAtUtc >= today),
            NotificationsSentTodayCount: notifications.Count,
            RecentActiveAlerts: activeAlerts.OrderByDescending(a => a.TriggeredAtUtc).Take(10).Select(MapToAlertDto).ToList(),
            RecentDispatches: notifications.OrderByDescending(n => n.SentAtUtc).Take(10).Select(MapToNotificationDto).ToList());
    }

    public async Task<PagedResult<AlertDto>> GetAlertsPagedAsync(AlertFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .Where(a => a.TenantId == tenantId);

        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);
        if (request.Severity.HasValue)
            query = query.Where(a => a.Severity == request.Severity.Value);
        if (request.SourceType.HasValue)
            query = query.Where(a => a.SourceType == request.SourceType.Value);
        if (request.VehicleId.HasValue)
            query = query.Where(a => a.VehicleId == request.VehicleId.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(term) || a.Message.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.TriggeredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AlertDto>(
            items.Select(MapToAlertDto).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<AlertDto?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var alert = await _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        return alert == null ? null : MapToAlertDto(alert);
    }

    public async Task<AlertDto> AcknowledgeAlertAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var alert = await _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (alert == null)
            throw new KeyNotFoundException($"Alert with ID '{id}' was not found.");

        alert.Acknowledge(_currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAlertDto(alert);
    }

    public async Task<AlertDto> ResolveAlertAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var alert = await _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (alert == null)
            throw new KeyNotFoundException($"Alert with ID '{id}' was not found.");

        alert.Resolve(_currentUserContext.UserId ?? Guid.Empty, notes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAlertDto(alert);
    }

    public async Task<AlertDto> DismissAlertAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var alert = await _dbContext.Alerts
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (alert == null)
            throw new KeyNotFoundException($"Alert with ID '{id}' was not found.");

        alert.Dismiss(_currentUserContext.UserId ?? Guid.Empty, reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAlertDto(alert);
    }

    public async Task<IReadOnlyCollection<AlertRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var rules = await _dbContext.AlertRules
            .Include(r => r.Conditions)
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task<AlertRuleDto> CreateRuleAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var rule = new AlertRule(
            tenantId: tenantId,
            code: request.Code,
            name: request.Name,
            sourceType: request.SourceType,
            severity: request.Severity,
            cooldownMinutes: request.CooldownMinutes,
            description: request.Description,
            isEnabled: true,
            createdByUserId: _currentUserContext.UserId);

        if (request.Conditions != null)
        {
            foreach (var cond in request.Conditions)
            {
                rule.AddCondition(new AlertRuleCondition(
                    tenantId: tenantId,
                    alertRuleId: rule.Id,
                    fieldName: cond.FieldName,
                    op: cond.Operator,
                    thresholdValue: cond.ThresholdValue,
                    sortOrder: cond.SortOrder));
            }
        }

        _dbContext.AlertRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToRuleDto(rule);
    }

    public async Task<AlertRuleDto> UpdateRuleAsync(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var rule = await _dbContext.AlertRules
            .Include(r => r.Conditions)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (rule == null)
            throw new KeyNotFoundException($"AlertRule with ID '{id}' was not found.");

        rule.Update(request.Name, request.Severity, request.CooldownMinutes, request.Description, _currentUserContext.UserId);
        rule.SetEnabled(request.IsEnabled, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToRuleDto(rule);
    }

    public async Task<NotificationMessageDto> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        // Multi-channel dispatching simulation (supports InApp, Email, SMS, Webhook, WhatsApp, Telegram)
        string externalId = $"{request.Channel.ToString().ToUpperInvariant()}-{Guid.NewGuid():N}"[..18];

        _logger.LogInformation("Dispatching operational notification via {Channel} to {Recipient}: {Title}",
            request.Channel, request.Recipient, request.Title);

        var msg = new NotificationMessage(
            tenantId: tenantId,
            channel: request.Channel,
            recipient: request.Recipient,
            title: request.Title,
            body: request.Body,
            alertId: request.AlertId,
            isDelivered: true,
            externalMessageId: externalId);

        _dbContext.NotificationMessages.Add(msg);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToNotificationDto(msg);
    }

    public async Task<IReadOnlyCollection<NotificationMessageDto>> GetRecentNotificationsAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var msgs = await _dbContext.NotificationMessages
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.SentAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

        return msgs.Select(MapToNotificationDto).ToList();
    }

    private static AlertDto MapToAlertDto(Alert a) =>
        new(a.Id,
            a.Title,
            a.Message,
            a.Severity,
            a.Severity.ToString(),
            a.Status,
            a.Status.ToString(),
            a.SourceType,
            a.SourceType.ToString(),
            a.VehicleId,
            a.Vehicle?.RegistrationNumber ?? a.Vehicle?.VehicleNumber,
            a.DriverId,
            a.Driver?.DisplayName,
            a.Latitude,
            a.Longitude,
            a.TriggeredAtUtc,
            a.AcknowledgedAtUtc,
            a.ResolvedAtUtc,
            a.ResolutionNotes);

    private static AlertRuleDto MapToRuleDto(AlertRule r) =>
        new(r.Id,
            r.Code,
            r.Name,
            r.Description,
            r.SourceType,
            r.SourceType.ToString(),
            r.Severity,
            r.Severity.ToString(),
            r.CooldownMinutes,
            r.IsEnabled,
            r.Conditions.Select(c => new AlertRuleConditionDto(
                c.Id,
                c.FieldName,
                c.Operator,
                c.Operator.ToString(),
                c.ThresholdValue,
                c.SortOrder)).ToList());

    private static NotificationMessageDto MapToNotificationDto(NotificationMessage n) =>
        new(n.Id,
            n.Channel,
            n.Channel.ToString(),
            n.Recipient,
            n.Title,
            n.Body,
            n.IsDelivered,
            n.ExternalMessageId,
            n.SentAtUtc);
}

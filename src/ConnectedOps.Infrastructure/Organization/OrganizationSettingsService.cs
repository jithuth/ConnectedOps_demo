using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class OrganizationSettingsService : IOrganizationSettingsService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public OrganizationSettingsService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<OrganizationSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var settings = await _dbContext.OrganizationSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings is null)
        {
            settings = new OrganizationSettings(tenantId);
            _dbContext.OrganizationSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(settings);
    }

    public async Task<OrganizationSettingsDto> UpdateSettingsAsync(
        UpdateOrganizationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var settings = await _dbContext.OrganizationSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings is null)
        {
            settings = new OrganizationSettings(
                tenantId,
                request.EnforceBranchAssignment,
                request.EnforceDepartmentAssignment,
                request.AutoCreateEmployeeForUser,
                request.FiscalYearStartMonth,
                request.DefaultWorkingDaysJson);

            _dbContext.OrganizationSettings.Add(settings);
        }
        else
        {
            settings.Update(
                request.EnforceBranchAssignment,
                request.EnforceDepartmentAssignment,
                request.AutoCreateEmployeeForUser,
                request.FiscalYearStartMonth,
                request.DefaultWorkingDaysJson);
        }

        settings.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.SettingsUpdated,
                "OrganizationSettings",
                settings.Id.ToString(),
                "Updated organization settings"),
            cancellationToken);

        return MapToDto(settings);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static OrganizationSettingsDto MapToDto(OrganizationSettings settings) =>
        new(
            settings.Id,
            settings.TenantId,
            settings.EnforceBranchAssignment,
            settings.EnforceDepartmentAssignment,
            settings.AutoCreateEmployeeForUser,
            settings.FiscalYearStartMonth,
            settings.DefaultWorkingDaysJson,
            settings.CreatedAtUtc,
            settings.UpdatedAtUtc);
}

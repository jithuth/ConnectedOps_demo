using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceProviderService : IMaintenanceProviderService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public MaintenanceProviderService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<MaintenanceProviderDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.MaintenanceProviders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var list = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<MaintenanceProviderDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.MaintenanceProviders
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Maintenance provider '{id}' was not found.");

        return MapToDto(item);
    }

    public async Task<MaintenanceProviderDto> CreateAsync(
        CreateMaintenanceProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.MaintenanceProviders
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance provider with code '{normalizedCode}' already exists for this tenant.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);

            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found for this tenant.");
        }

        var provider = new MaintenanceProvider(
            tenantId,
            normalizedCode,
            request.Name,
            request.ProviderType,
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.Address,
            request.BranchId,
            request.Notes,
            request.IsActive);

        _dbContext.MaintenanceProviders.Add(provider);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceProviderCreated,
                "MaintenanceProvider",
                provider.Id.ToString(),
                $"Created maintenance provider '{provider.Name}' ({provider.Code})"),
            cancellationToken);

        return await GetByIdAsync(provider.Id, cancellationToken);
    }

    public async Task<MaintenanceProviderDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var provider = await _dbContext.MaintenanceProviders
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (provider is null)
            throw new KeyNotFoundException($"Maintenance provider '{id}' was not found.");

        var codeExists = await _dbContext.MaintenanceProviders
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance provider with code '{normalizedCode}' already exists for this tenant.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);

            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found for this tenant.");
        }

        provider.Update(
            normalizedCode,
            request.Name,
            request.ProviderType,
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.Address,
            request.BranchId,
            request.Notes,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceProviderUpdated,
                "MaintenanceProvider",
                provider.Id.ToString(),
                $"Updated maintenance provider '{provider.Name}' ({provider.Code})"),
            cancellationToken);

        return await GetByIdAsync(provider.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var provider = await _dbContext.MaintenanceProviders
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (provider is null)
            throw new KeyNotFoundException($"Maintenance provider '{id}' was not found.");

        provider.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceProviderDeleted,
                "MaintenanceProvider",
                provider.Id.ToString(),
                $"Deleted maintenance provider '{provider.Name}' ({provider.Code})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static MaintenanceProviderDto MapToDto(MaintenanceProvider entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.ProviderType,
            entity.ProviderType.ToString(),
            entity.ContactPerson,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.BranchId,
            entity.Branch?.Name,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}

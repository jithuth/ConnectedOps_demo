using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelTypeService : IFuelTypeService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FuelTypeService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<FuelTypeDefinitionDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var query = _dbContext.FuelTypeDefinitions
            .AsNoTracking()
            .Where(x => x.TenantId == null || x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var list = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<FuelTypeDefinitionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var item = await _dbContext.FuelTypeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && (x.TenantId == null || x.TenantId == tenantId), cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Fuel type definition '{id}' was not found.");

        return MapToDto(item);
    }

    public async Task<FuelTypeDefinitionDto> CreateAsync(
        CreateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.FuelTypeDefinitions
            .AnyAsync(x => (x.TenantId == tenantId || x.TenantId == null) && x.Code == normalizedCode, cancellationToken);

        if (exists)
            throw new ConflictException($"Fuel type definition with code '{normalizedCode}' already exists.");

        var entity = new FuelTypeDefinition(
            tenantId,
            normalizedCode,
            request.Name,
            request.FuelType,
            request.EnergyType,
            request.DefaultUnit,
            request.Density,
            request.IsActive);

        _dbContext.FuelTypeDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<FuelTypeDefinitionDto> UpdateAsync(
        Guid id,
        UpdateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var entity = await _dbContext.FuelTypeDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity is null)
            throw new KeyNotFoundException($"Fuel type definition '{id}' was not found or is a system definition.");

        var exists = await _dbContext.FuelTypeDefinitions
            .AnyAsync(x => (x.TenantId == tenantId || x.TenantId == null) && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (exists)
            throw new ConflictException($"Fuel type definition with code '{normalizedCode}' already exists.");

        entity.Update(
            normalizedCode,
            request.Name,
            request.FuelType,
            request.EnergyType,
            request.DefaultUnit,
            request.Density,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var entity = await _dbContext.FuelTypeDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity is null)
            throw new KeyNotFoundException($"Fuel type definition '{id}' was not found or is a system definition.");

        _dbContext.FuelTypeDefinitions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static FuelTypeDefinitionDto MapToDto(FuelTypeDefinition entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.FuelType,
            entity.FuelType.ToString(),
            entity.EnergyType,
            entity.DefaultUnit,
            entity.DefaultUnit.ToString(),
            entity.Density,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}

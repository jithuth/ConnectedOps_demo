using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleMakeModelService : IVehicleMakeModelService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public VehicleMakeModelService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    // Makes
    public async Task<IReadOnlyCollection<VehicleMakeDto>> GetMakesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleMakes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var makes = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                Make = x,
                ModelCount = x.Models.Count(m => !m.IsDeleted),
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleMakeId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return makes.Select(x => new VehicleMakeDto(
            x.Make.Id,
            x.Make.TenantId,
            x.Make.Name,
            x.Make.CountryCode,
            x.Make.IsActive,
            x.ModelCount,
            x.VehicleCount)).ToList();
    }

    public async Task<VehicleMakeDto> GetMakeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.VehicleMakes
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Make = x,
                ModelCount = x.Models.Count(m => !m.IsDeleted),
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleMakeId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Vehicle make '{id}' was not found.");

        return new VehicleMakeDto(
            item.Make.Id,
            item.Make.TenantId,
            item.Make.Name,
            item.Make.CountryCode,
            item.Make.IsActive,
            item.ModelCount,
            item.VehicleCount);
    }

    public async Task<VehicleMakeDto> CreateMakeAsync(
        CreateVehicleMakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedName = request.Name.Trim();

        var nameExists = await _dbContext.VehicleMakes
            .AnyAsync(x => x.TenantId == tenantId && x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (nameExists)
            throw new ConflictException($"Vehicle make '{normalizedName}' already exists.");

        var make = new VehicleMake(tenantId, normalizedName, request.CountryCode);

        _dbContext.VehicleMakes.Add(make);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "VehicleMake",
                make.Id.ToString(),
                $"Created vehicle make: {make.Name}"),
            cancellationToken);

        return new VehicleMakeDto(
            make.Id,
            make.TenantId,
            make.Name,
            make.CountryCode,
            make.IsActive,
            0,
            0);
    }

    public async Task<VehicleMakeDto> UpdateMakeAsync(
        Guid id,
        UpdateVehicleMakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedName = request.Name.Trim();

        var make = await _dbContext.VehicleMakes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (make is null)
            throw new KeyNotFoundException($"Vehicle make '{id}' was not found.");

        var nameExists = await _dbContext.VehicleMakes
            .AnyAsync(x => x.TenantId == tenantId && x.Id != id && x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (nameExists)
            throw new ConflictException($"Vehicle make '{normalizedName}' already exists.");

        make.Update(normalizedName, request.CountryCode);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "VehicleMake",
                make.Id.ToString(),
                $"Updated vehicle make: {make.Name}"),
            cancellationToken);

        var modelCount = await _dbContext.VehicleModels
            .CountAsync(m => m.TenantId == tenantId && m.VehicleMakeId == id && !m.IsDeleted, cancellationToken);
        var vehicleCount = await _dbContext.Vehicles
            .CountAsync(v => v.TenantId == tenantId && v.VehicleMakeId == id, cancellationToken);

        return new VehicleMakeDto(
            make.Id,
            make.TenantId,
            make.Name,
            make.CountryCode,
            make.IsActive,
            modelCount,
            vehicleCount);
    }

    public async Task DeleteMakeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var make = await _dbContext.VehicleMakes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (make is null)
            throw new KeyNotFoundException($"Vehicle make '{id}' was not found.");

        var hasVehicles = await _dbContext.Vehicles
            .AnyAsync(v => v.TenantId == tenantId && v.VehicleMakeId == id, cancellationToken);

        if (hasVehicles)
            throw new ConflictException("Cannot delete make because vehicles are associated with it.");

        var hasModels = await _dbContext.VehicleModels
            .AnyAsync(m => m.TenantId == tenantId && m.VehicleMakeId == id && !m.IsDeleted, cancellationToken);

        if (hasModels)
            throw new ConflictException("Cannot delete make because models are registered under it.");

        make.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "VehicleMake",
                make.Id.ToString(),
                $"Deleted vehicle make: {make.Name}"),
            cancellationToken);
    }

    // Models
    public async Task<IReadOnlyCollection<VehicleModelDto>> GetModelsByMakeIdAsync(
        Guid makeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleModels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.VehicleMakeId == makeId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var models = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                Model = x,
                MakeName = x.VehicleMake.Name,
                DefaultCategoryName = x.DefaultCategory != null ? x.DefaultCategory.Name : null,
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleModelId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return models.Select(x => new VehicleModelDto(
            x.Model.Id,
            x.Model.TenantId,
            x.Model.VehicleMakeId,
            x.MakeName,
            x.Model.Name,
            x.Model.DefaultCategoryId,
            x.DefaultCategoryName,
            x.Model.IsActive,
            x.VehicleCount)).ToList();
    }

    public async Task<IReadOnlyCollection<VehicleModelDto>> GetAllModelsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleModels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var models = await query
            .OrderBy(x => x.VehicleMake.Name)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Model = x,
                MakeName = x.VehicleMake.Name,
                DefaultCategoryName = x.DefaultCategory != null ? x.DefaultCategory.Name : null,
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleModelId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return models.Select(x => new VehicleModelDto(
            x.Model.Id,
            x.Model.TenantId,
            x.Model.VehicleMakeId,
            x.MakeName,
            x.Model.Name,
            x.Model.DefaultCategoryId,
            x.DefaultCategoryName,
            x.Model.IsActive,
            x.VehicleCount)).ToList();
    }

    public async Task<VehicleModelDto> GetModelByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.VehicleModels
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Model = x,
                MakeName = x.VehicleMake.Name,
                DefaultCategoryName = x.DefaultCategory != null ? x.DefaultCategory.Name : null,
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleModelId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Vehicle model '{id}' was not found.");

        return new VehicleModelDto(
            item.Model.Id,
            item.Model.TenantId,
            item.Model.VehicleMakeId,
            item.MakeName,
            item.Model.Name,
            item.Model.DefaultCategoryId,
            item.DefaultCategoryName,
            item.Model.IsActive,
            item.VehicleCount);
    }

    public async Task<VehicleModelDto> CreateModelAsync(
        CreateVehicleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedName = request.Name.Trim();

        var makeExists = await _dbContext.VehicleMakes
            .AnyAsync(x => x.Id == request.VehicleMakeId && x.TenantId == tenantId, cancellationToken);

        if (!makeExists)
            throw new KeyNotFoundException($"Vehicle make '{request.VehicleMakeId}' was not found.");

        var nameExists = await _dbContext.VehicleModels
            .AnyAsync(x => x.TenantId == tenantId && x.VehicleMakeId == request.VehicleMakeId && x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (nameExists)
            throw new ConflictException($"Model '{normalizedName}' already exists for this make.");

        if (request.DefaultCategoryId.HasValue)
        {
            var categoryExists = await _dbContext.VehicleCategories
                .AnyAsync(x => x.Id == request.DefaultCategoryId.Value && x.TenantId == tenantId, cancellationToken);

            if (!categoryExists)
                throw new KeyNotFoundException($"Vehicle category '{request.DefaultCategoryId.Value}' was not found.");
        }

        var model = new VehicleModel(
            tenantId,
            request.VehicleMakeId,
            normalizedName,
            request.DefaultCategoryId);

        _dbContext.VehicleModels.Add(model);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "VehicleModel",
                model.Id.ToString(),
                $"Created vehicle model: {model.Name}"),
            cancellationToken);

        var make = await _dbContext.VehicleMakes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.VehicleMakeId, cancellationToken);

        string? categoryName = null;
        if (request.DefaultCategoryId.HasValue)
        {
            var cat = await _dbContext.VehicleCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.DefaultCategoryId.Value, cancellationToken);
            categoryName = cat?.Name;
        }

        return new VehicleModelDto(
            model.Id,
            model.TenantId,
            model.VehicleMakeId,
            make?.Name ?? string.Empty,
            model.Name,
            model.DefaultCategoryId,
            categoryName,
            model.IsActive,
            0);
    }

    public async Task<VehicleModelDto> UpdateModelAsync(
        Guid id,
        UpdateVehicleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedName = request.Name.Trim();

        var model = await _dbContext.VehicleModels
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (model is null)
            throw new KeyNotFoundException($"Vehicle model '{id}' was not found.");

        var nameExists = await _dbContext.VehicleModels
            .AnyAsync(x => x.TenantId == tenantId && x.VehicleMakeId == model.VehicleMakeId && x.Id != id && x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (nameExists)
            throw new ConflictException($"Model '{normalizedName}' already exists for this make.");

        if (request.DefaultCategoryId.HasValue)
        {
            var categoryExists = await _dbContext.VehicleCategories
                .AnyAsync(x => x.Id == request.DefaultCategoryId.Value && x.TenantId == tenantId, cancellationToken);

            if (!categoryExists)
                throw new KeyNotFoundException($"Vehicle category '{request.DefaultCategoryId.Value}' was not found.");
        }

        model.Update(normalizedName, request.DefaultCategoryId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "VehicleModel",
                model.Id.ToString(),
                $"Updated vehicle model: {model.Name}"),
            cancellationToken);

        var make = await _dbContext.VehicleMakes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.VehicleMakeId, cancellationToken);

        string? categoryName = null;
        if (request.DefaultCategoryId.HasValue)
        {
            var cat = await _dbContext.VehicleCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.DefaultCategoryId.Value, cancellationToken);
            categoryName = cat?.Name;
        }

        var vehicleCount = await _dbContext.Vehicles
            .CountAsync(v => v.TenantId == tenantId && v.VehicleModelId == id, cancellationToken);

        return new VehicleModelDto(
            model.Id,
            model.TenantId,
            model.VehicleMakeId,
            make?.Name ?? string.Empty,
            model.Name,
            model.DefaultCategoryId,
            categoryName,
            model.IsActive,
            vehicleCount);
    }

    public async Task DeleteModelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var model = await _dbContext.VehicleModels
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (model is null)
            throw new KeyNotFoundException($"Vehicle model '{id}' was not found.");

        var hasVehicles = await _dbContext.Vehicles
            .AnyAsync(v => v.TenantId == tenantId && v.VehicleModelId == id, cancellationToken);

        if (hasVehicles)
            throw new ConflictException("Cannot delete model because vehicles are associated with it.");

        model.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "VehicleModel",
                model.Id.ToString(),
                $"Deleted vehicle model: {model.Name}"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}

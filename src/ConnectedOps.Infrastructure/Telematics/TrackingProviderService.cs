using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TrackingProviderService : ITrackingProviderService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TrackingProviderService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<TrackingProviderDto>> GetProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var providers = await _dbContext.TrackingProviders
            .AsNoTracking()
            .Where(x => x.TenantId == null || x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new TrackingProviderDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.ProviderType,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return providers;
    }

    public async Task<TrackingProviderDto?> GetProviderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var provider = await _dbContext.TrackingProviders
            .AsNoTracking()
            .Where(x => x.Id == id && (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new TrackingProviderDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.ProviderType,
                x.Description,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return provider;
    }

    public async Task<TrackingProviderDto?> GetProviderByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var tenantId = _currentUserContext.TenantId;

        var provider = await _dbContext.TrackingProviders
            .AsNoTracking()
            .Where(x => x.Code == normalizedCode && (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new TrackingProviderDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.ProviderType,
                x.Description,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return provider;
    }

    public async Task<TrackingProviderDto> CreateProviderAsync(
        CreateTrackingProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.TrackingProviders
            .AnyAsync(x => x.Code == normalizedCode && (x.TenantId == null || x.TenantId == tenantId), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Provider with code '{normalizedCode}' already exists.");
        }

        var provider = new TrackingProvider(
            request.Name,
            normalizedCode,
            request.ProviderType,
            request.Description,
            tenantId);

        _dbContext.TrackingProviders.Add(provider);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TrackingProviderDto(
            provider.Id,
            provider.TenantId,
            provider.Name,
            provider.Code,
            provider.ProviderType,
            provider.Description,
            provider.IsActive);
    }

    public async Task<IReadOnlyCollection<TrackingDeviceTypeDto>> GetDeviceTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var types = await _dbContext.TrackingDeviceTypes
            .AsNoTracking()
            .Where(x => x.TenantId == null || x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new TrackingDeviceTypeDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.Description,
                x.SupportsGps,
                x.SupportsIgnition,
                x.SupportsCanBus,
                x.SupportsObd,
                x.SupportsBattery,
                x.SupportsTemperature,
                x.SupportsFuel,
                x.SupportsBle,
                x.SupportsCommands,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return types;
    }

    public async Task<TrackingDeviceTypeDto?> GetDeviceTypeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var type = await _dbContext.TrackingDeviceTypes
            .AsNoTracking()
            .Where(x => x.Id == id && (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new TrackingDeviceTypeDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.Description,
                x.SupportsGps,
                x.SupportsIgnition,
                x.SupportsCanBus,
                x.SupportsObd,
                x.SupportsBattery,
                x.SupportsTemperature,
                x.SupportsFuel,
                x.SupportsBle,
                x.SupportsCommands,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return type;
    }

    public async Task<TrackingDeviceTypeDto?> GetDeviceTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var tenantId = _currentUserContext.TenantId;

        var type = await _dbContext.TrackingDeviceTypes
            .AsNoTracking()
            .Where(x => x.Code == normalizedCode && (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new TrackingDeviceTypeDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.Description,
                x.SupportsGps,
                x.SupportsIgnition,
                x.SupportsCanBus,
                x.SupportsObd,
                x.SupportsBattery,
                x.SupportsTemperature,
                x.SupportsFuel,
                x.SupportsBle,
                x.SupportsCommands,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return type;
    }

    public async Task<TrackingDeviceTypeDto> CreateDeviceTypeAsync(
        CreateTrackingDeviceTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.TrackingDeviceTypes
            .AnyAsync(x => x.Code == normalizedCode && (x.TenantId == null || x.TenantId == tenantId), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Device type with code '{normalizedCode}' already exists.");
        }

        var deviceType = new TrackingDeviceType(
            request.Name,
            normalizedCode,
            request.Description,
            tenantId,
            request.SupportsGps,
            request.SupportsIgnition,
            request.SupportsCanBus,
            request.SupportsObd,
            request.SupportsBattery,
            request.SupportsTemperature,
            request.SupportsFuel,
            request.SupportsBle,
            request.SupportsCommands);

        _dbContext.TrackingDeviceTypes.Add(deviceType);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TrackingDeviceTypeDto(
            deviceType.Id,
            deviceType.TenantId,
            deviceType.Name,
            deviceType.Code,
            deviceType.Description,
            deviceType.SupportsGps,
            deviceType.SupportsIgnition,
            deviceType.SupportsCanBus,
            deviceType.SupportsObd,
            deviceType.SupportsBattery,
            deviceType.SupportsTemperature,
            deviceType.SupportsFuel,
            deviceType.SupportsBle,
            deviceType.SupportsCommands,
            deviceType.IsActive);
    }
}

using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelCardService : IFuelCardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FuelCardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<FuelCardListItemDto>> GetCardsPagedAsync(
        FuelCardQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelCards
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Where(x => x.TenantId == tenantId);

        if (parameters.Status.HasValue)
        {
            query = query.Where(x => x.Status == parameters.Status.Value);
        }

        if (parameters.VehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == parameters.VehicleId.Value);
        }

        if (parameters.DriverId.HasValue)
        {
            query = query.Where(x => x.DriverId == parameters.DriverId.Value);
        }

        if (parameters.ActiveOnly.HasValue && parameters.ActiveOnly.Value)
        {
            query = query.Where(x => x.IsActive && x.Status == FuelCardStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var search = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.CardReference.ToLower().Contains(search) ||
                x.ProviderName.ToLower().Contains(search) ||
                x.CardNumberMasked.ToLower().Contains(search) ||
                (x.Vehicle != null && x.Vehicle.VehicleNumber.ToLower().Contains(search)) ||
                (x.Driver != null && (x.Driver.FirstName.ToLower().Contains(search) || x.Driver.LastName.ToLower().Contains(search))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FuelCardListItemDto(
                x.Id,
                x.CardNumberMasked,
                x.CardReference,
                x.ProviderName,
                x.VehicleId,
                x.Vehicle != null ? x.Vehicle.VehicleNumber : null,
                x.DriverId,
                x.Driver != null ? $"{x.Driver.FirstName} {x.Driver.LastName}" : null,
                x.ExpiresAtUtc,
                x.Status,
                x.Status.ToString(),
                x.SpendingLimit,
                x.CurrencyCode,
                x.IsActive,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<FuelCardListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<FuelCardDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelCards
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Where(x => x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive && x.Status == FuelCardStatus.Active);
        }

        var list = await query
            .OrderBy(x => x.CardReference)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<FuelCardDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.FuelCards
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Fuel card '{id}' was not found.");

        return MapToDto(item);
    }

    public async Task<FuelCardDto> CreateAsync(
        CreateFuelCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        if (request.VehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.VehicleId.Value, cancellationToken);
            if (!vehicleExists)
                throw new ValidationException($"Vehicle '{request.VehicleId.Value}' was not found.");
        }

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.DriverId.Value, cancellationToken);
            if (!driverExists)
                throw new ValidationException($"Driver '{request.DriverId.Value}' was not found.");
        }

        var card = new FuelCard(
            tenantId,
            request.CardNumber,
            request.CardReference,
            request.ProviderName,
            request.VehicleId,
            request.DriverId,
            request.IssuedAtUtc,
            request.ExpiresAtUtc,
            request.Status,
            request.SpendingLimit,
            request.CurrencyCode ?? "USD",
            request.Notes,
            request.IsActive);

        _dbContext.FuelCards.Add(card);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelCardCreated,
                "FuelCard",
                card.Id.ToString(),
                $"Created fuel card '{card.CardReference}' ({card.CardNumberMasked})"),
            cancellationToken);

        return await GetByIdAsync(card.Id, cancellationToken);
    }

    public async Task<FuelCardDto> UpdateAsync(
        Guid id,
        UpdateFuelCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var card = await _dbContext.FuelCards
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (card is null)
            throw new KeyNotFoundException($"Fuel card '{id}' was not found.");

        if (request.VehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.VehicleId.Value, cancellationToken);
            if (!vehicleExists)
                throw new ValidationException($"Vehicle '{request.VehicleId.Value}' was not found.");
        }

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.DriverId.Value, cancellationToken);
            if (!driverExists)
                throw new ValidationException($"Driver '{request.DriverId.Value}' was not found.");
        }

        card.Update(
            request.CardNumber,
            request.CardReference,
            request.ProviderName,
            request.VehicleId,
            request.DriverId,
            request.IssuedAtUtc,
            request.ExpiresAtUtc,
            request.Status,
            request.SpendingLimit,
            request.CurrencyCode ?? "USD",
            request.Notes,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelCardUpdated,
                "FuelCard",
                card.Id.ToString(),
                $"Updated fuel card '{card.CardReference}' ({card.CardNumberMasked})"),
            cancellationToken);

        return await GetByIdAsync(card.Id, cancellationToken);
    }

    public async Task<FuelCardDto> SetStatusAsync(
        Guid id,
        FuelCardStatus status,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var card = await _dbContext.FuelCards
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (card is null)
            throw new KeyNotFoundException($"Fuel card '{id}' was not found.");

        card.SetStatus(status, _currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var auditAction = status == FuelCardStatus.Suspended ? AuditAction.FuelCardSuspended : AuditAction.FuelCardUpdated;

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                auditAction,
                "FuelCard",
                card.Id.ToString(),
                $"Changed fuel card '{card.CardReference}' status to {status}"),
            cancellationToken);

        return await GetByIdAsync(card.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var card = await _dbContext.FuelCards
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (card is null)
            throw new KeyNotFoundException($"Fuel card '{id}' was not found.");

        card.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelCardDeleted,
                "FuelCard",
                card.Id.ToString(),
                $"Deleted fuel card '{card.CardReference}' ({card.CardNumberMasked})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static FuelCardDto MapToDto(FuelCard entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.CardNumberMasked,
            entity.CardReference,
            entity.ProviderName,
            entity.VehicleId,
            entity.Vehicle?.VehicleNumber,
            entity.Vehicle?.DisplayName,
            entity.DriverId,
            entity.Driver != null ? $"{entity.Driver.FirstName} {entity.Driver.LastName}" : null,
            entity.IssuedAtUtc,
            entity.ExpiresAtUtc,
            entity.Status,
            entity.Status.ToString(),
            entity.SpendingLimit,
            entity.CurrencyCode,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}

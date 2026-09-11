using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleOdometerService : IVehicleOdometerService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public VehicleOdometerService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<VehicleOdometerEntryDto>> GetOdometerHistoryAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (!vehicleExists)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var entries = await _dbContext.VehicleOdometerEntries
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId)
            .OrderByDescending(x => x.ReadingDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entries.Select(x => new VehicleOdometerEntryDto(
            x.Id,
            x.VehicleId,
            x.Reading,
            x.Unit,
            x.Unit.ToString(),
            x.ReadingDateUtc,
            x.Source,
            x.Source.ToString(),
            x.Notes,
            x.RecordedByUserId,
            x.CreatedAtUtc)).ToList();
    }

    public async Task<VehicleOdometerEntryDto> RecordOdometerAsync(
        Guid vehicleId,
        RecordVehicleOdometerRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        if (request.Reading < vehicle.CurrentOdometer)
        {
            throw new InvalidOperationException(
                $"New odometer reading ({request.Reading}) cannot be less than current odometer reading ({vehicle.CurrentOdometer}).");
        }

        var entry = new VehicleOdometerEntry(
            tenantId,
            vehicleId,
            request.Reading,
            request.Unit,
            request.ReadingDateUtc,
            request.Source,
            request.Notes,
            userId);

        vehicle.UpdateOdometer(request.Reading);

        _dbContext.VehicleOdometerEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.OdometerRecorded,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Recorded odometer for vehicle {vehicle.VehicleNumber}: {entry.Reading} {entry.Unit} ({entry.Source})"),
            cancellationToken);

        return new VehicleOdometerEntryDto(
            entry.Id,
            entry.VehicleId,
            entry.Reading,
            entry.Unit,
            entry.Unit.ToString(),
            entry.ReadingDateUtc,
            entry.Source,
            entry.Source.ToString(),
            entry.Notes,
            entry.RecordedByUserId,
            entry.CreatedAtUtc);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}

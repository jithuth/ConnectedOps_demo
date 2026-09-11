using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetShiftAssignmentService : IFleetShiftAssignmentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FleetShiftAssignmentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<FleetShiftAssignmentDto>> GetAssignmentsAsync(
        DateOnly? date = null,
        Guid? shiftId = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FleetShiftAssignments
            .AsNoTracking()
            .Include(a => a.FleetShift)
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId);

        if (date.HasValue)
            query = query.Where(a => a.AssignmentDate == date.Value);

        if (shiftId.HasValue)
            query = query.Where(a => a.FleetShiftId == shiftId.Value);

        if (driverId.HasValue)
            query = query.Where(a => a.DriverId == driverId.Value);

        if (vehicleId.HasValue)
            query = query.Where(a => a.VehicleId == vehicleId.Value);

        var assignments = await query
            .OrderByDescending(a => a.AssignmentDate)
            .ThenBy(a => a.StartDateTimeUtc)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<FleetShiftAssignmentDto> GetAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.FleetShiftAssignments
            .AsNoTracking()
            .Include(a => a.FleetShift)
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Shift assignment '{id}' was not found.");

        return MapToDto(assignment);
    }

    public async Task<FleetShiftAssignmentDto> CreateAssignmentAsync(
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shiftExists = await _dbContext.FleetShifts
            .AnyAsync(s => s.Id == request.FleetShiftId && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);
        if (!shiftExists)
            throw new KeyNotFoundException($"Fleet shift '{request.FleetShiftId}' was not found.");

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers
                .AnyAsync(d => d.Id == request.DriverId.Value && d.TenantId == tenantId, cancellationToken);
            if (!driverExists)
                throw new KeyNotFoundException($"Driver '{request.DriverId.Value}' was not found.");
        }

        if (request.VehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles
                .AnyAsync(v => v.Id == request.VehicleId.Value && v.TenantId == tenantId, cancellationToken);
            if (!vehicleExists)
                throw new KeyNotFoundException($"Vehicle '{request.VehicleId.Value}' was not found.");
        }

        var assignment = new FleetShiftAssignment(
            tenantId,
            request.FleetShiftId,
            request.AssignmentDate,
            request.StartDateTimeUtc,
            request.EndDateTimeUtc,
            request.DriverId,
            request.VehicleId,
            ShiftAssignmentStatus.Planned,
            request.Notes);

        _dbContext.FleetShiftAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftAssignmentCreated,
                "FleetShiftAssignment",
                assignment.Id.ToString(),
                $"Created shift assignment on {request.AssignmentDate:yyyy-MM-dd}"),
            cancellationToken);

        return await GetAssignmentByIdAsync(assignment.Id, cancellationToken);
    }

    public async Task<FleetShiftAssignmentDto> UpdateAssignmentAsync(
        Guid id,
        UpdateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.FleetShiftAssignments
            .Include(a => a.FleetShift)
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Shift assignment '{id}' was not found.");

        assignment.UpdateAssignment(
            request.DriverId,
            request.VehicleId,
            request.StartDateTimeUtc,
            request.EndDateTimeUtc,
            request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftAssignmentUpdated,
                "FleetShiftAssignment",
                assignment.Id.ToString(),
                $"Updated shift assignment {id}"),
            cancellationToken);

        return MapToDto(assignment);
    }

    public async Task ActivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.FleetShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Shift assignment '{id}' was not found.");

        assignment.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftAssignmentActivated,
                "FleetShiftAssignment",
                assignment.Id.ToString(),
                $"Activated shift assignment {id}"),
            cancellationToken);
    }

    public async Task CompleteAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.FleetShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Shift assignment '{id}' was not found.");

        assignment.Complete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftAssignmentCompleted,
                "FleetShiftAssignment",
                assignment.Id.ToString(),
                $"Completed shift assignment {id}"),
            cancellationToken);
    }

    public async Task CancelAssignmentAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.FleetShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Shift assignment '{id}' was not found.");

        assignment.Cancel(reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftAssignmentCancelled,
                "FleetShiftAssignment",
                assignment.Id.ToString(),
                $"Cancelled shift assignment {id}: {reason}"),
            cancellationToken);
    }

    private static FleetShiftAssignmentDto MapToDto(FleetShiftAssignment a) =>
        new(
            a.Id,
            a.TenantId,
            a.FleetShiftId,
            a.FleetShift?.Name ?? string.Empty,
            a.FleetShift?.Code ?? string.Empty,
            a.AssignmentDate,
            a.StartDateTimeUtc,
            a.EndDateTimeUtc,
            a.DriverId,
            a.Driver?.DriverNumber,
            a.Driver?.DisplayName,
            a.VehicleId,
            a.Vehicle?.VehicleNumber,
            a.Vehicle?.DisplayName,
            a.AssignmentStatus,
            a.AssignmentStatus.ToString(),
            a.Notes,
            a.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class DeviceAssignmentService : IDeviceAssignmentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DeviceAssignmentService(
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
               throw new InvalidOperationException("Active tenant context is required for device assignment operations.");
    }

    public async Task<TrackingDeviceVehicleAssignmentDto> AssignDeviceToVehicleAsync(
        AssignDeviceToVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        // 1. Verify device belongs to tenant and is active
        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == request.TrackingDeviceId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Tracking device not found or does not belong to active organization.");

        if (!device.IsActive)
        {
            throw new InvalidOperationException("Cannot assign an inactive tracking device.");
        }

        // 2. Verify vehicle belongs to tenant
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found or does not belong to active organization.");

        // 3. Verify device is not already actively assigned
        var activeDeviceAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .FirstOrDefaultAsync(x => x.TrackingDeviceId == device.Id && x.IsActive && x.TenantId == tenantId, cancellationToken);

        if (activeDeviceAssignment != null)
        {
            throw new InvalidOperationException($"Tracking device '{device.DeviceIdentifier}' is already actively assigned to another vehicle.");
        }

        // 4. If primary assignment, check if vehicle already has active primary assignment
        if (request.IsPrimary)
        {
            var activePrimaryForVehicle = await _dbContext.TrackingDeviceVehicleAssignments
                .FirstOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.IsPrimary && x.IsActive && x.TenantId == tenantId, cancellationToken);

            if (activePrimaryForVehicle != null)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.VehicleNumber}' already has an active primary tracking device assigned.");
            }
        }

        var assignment = new TrackingDeviceVehicleAssignment(
            tenantId,
            device.Id,
            vehicle.Id,
            request.AssignedFromUtc ?? DateTime.UtcNow,
            request.IsPrimary,
            _currentUserContext.UserId,
            request.Notes);

        _dbContext.TrackingDeviceVehicleAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceAssignedToVehicle,
                "TrackingDeviceVehicleAssignment",
                assignment.Id.ToString(),
                $"Assigned device '{device.DeviceIdentifier}' to vehicle '{vehicle.VehicleNumber}'."),
            cancellationToken);

        return new TrackingDeviceVehicleAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.TrackingDeviceId,
            device.DeviceIdentifier,
            assignment.VehicleId,
            vehicle.VehicleNumber,
            vehicle.DisplayName ?? vehicle.VehicleNumber,
            assignment.AssignedFromUtc,
            assignment.AssignedToUtc,
            assignment.IsPrimary,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.EndedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task<TrackingDeviceVehicleAssignmentDto> EndDeviceAssignmentAsync(
        Guid assignmentId,
        EndDeviceAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignment = await _dbContext.TrackingDeviceVehicleAssignments
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (!assignment.IsActive)
        {
            throw new InvalidOperationException("Assignment is already ended.");
        }

        assignment.EndAssignment(_currentUserContext.UserId, request.EndedAtUtc ?? DateTime.UtcNow, request.Notes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceAssignmentEnded,
                "TrackingDeviceVehicleAssignment",
                assignment.Id.ToString(),
                $"Ended assignment for device '{assignment.TrackingDevice.DeviceIdentifier}' and vehicle '{assignment.Vehicle.VehicleNumber}'."),
            cancellationToken);

        return new TrackingDeviceVehicleAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.TrackingDeviceId,
            assignment.TrackingDevice.DeviceIdentifier,
            assignment.VehicleId,
            assignment.Vehicle.VehicleNumber,
            assignment.Vehicle.DisplayName ?? assignment.Vehicle.VehicleNumber,
            assignment.AssignedFromUtc,
            assignment.AssignedToUtc,
            assignment.IsPrimary,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.EndedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task<TrackingDeviceVehicleAssignmentDto?> GetActiveAssignmentForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsActive && x.TenantId == tenantId, cancellationToken);

        if (assignment == null)
            return null;

        return new TrackingDeviceVehicleAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.TrackingDeviceId,
            assignment.TrackingDevice.DeviceIdentifier,
            assignment.VehicleId,
            assignment.Vehicle.VehicleNumber,
            assignment.Vehicle.DisplayName ?? assignment.Vehicle.VehicleNumber,
            assignment.AssignedFromUtc,
            assignment.AssignedToUtc,
            assignment.IsPrimary,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.EndedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task<TrackingDeviceVehicleAssignmentDto?> GetActiveAssignmentForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.TrackingDeviceId == deviceId && x.IsActive && x.TenantId == tenantId, cancellationToken);

        if (assignment == null)
            return null;

        return new TrackingDeviceVehicleAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.TrackingDeviceId,
            assignment.TrackingDevice.DeviceIdentifier,
            assignment.VehicleId,
            assignment.Vehicle.VehicleNumber,
            assignment.Vehicle.DisplayName ?? assignment.Vehicle.VehicleNumber,
            assignment.AssignedFromUtc,
            assignment.AssignedToUtc,
            assignment.IsPrimary,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.EndedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<TrackingDeviceVehicleAssignmentDto>> GetAssignmentHistoryForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignments = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .Where(x => x.TrackingDeviceId == deviceId && x.TenantId == tenantId)
            .OrderByDescending(x => x.AssignedFromUtc)
            .Select(x => new TrackingDeviceVehicleAssignmentDto(
                x.Id,
                x.TenantId,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.VehicleId,
                x.Vehicle.VehicleNumber,
                x.Vehicle.DisplayName ?? x.Vehicle.VehicleNumber,
                x.AssignedFromUtc,
                x.AssignedToUtc,
                x.IsPrimary,
                x.IsActive,
                x.AssignedByUserId,
                x.EndedByUserId,
                x.Notes,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return assignments;
    }

    public async Task<IReadOnlyCollection<TrackingDeviceVehicleAssignmentDto>> GetAssignmentHistoryForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignments = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .Where(x => x.VehicleId == vehicleId && x.TenantId == tenantId)
            .OrderByDescending(x => x.AssignedFromUtc)
            .Select(x => new TrackingDeviceVehicleAssignmentDto(
                x.Id,
                x.TenantId,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.VehicleId,
                x.Vehicle.VehicleNumber,
                x.Vehicle.DisplayName ?? x.Vehicle.VehicleNumber,
                x.AssignedFromUtc,
                x.AssignedToUtc,
                x.IsPrimary,
                x.IsActive,
                x.AssignedByUserId,
                x.EndedByUserId,
                x.Notes,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return assignments;
    }
}

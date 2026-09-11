using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverAssignmentService : IDriverAssignmentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IDriverEligibilityService _eligibilityService;

    public DriverAssignmentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IDriverEligibilityService eligibilityService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _eligibilityService = eligibilityService;
    }

    public async Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetDriverAssignmentsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var assignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId && a.DriverId == driverId)
            .OrderByDescending(a => a.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetVehicleAssignmentsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);
        if (!vehicleExists)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var assignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId && a.VehicleId == vehicleId)
            .OrderByDescending(a => a.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetAllActiveAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;

        var assignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId &&
                        a.IsActive &&
                        a.AssignedFromUtc <= now &&
                        (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
            .OrderByDescending(a => a.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<DriverVehicleAssignmentDto?> GetActiveAssignmentForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;

        var assignment = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId &&
                        a.DriverId == driverId &&
                        a.IsActive &&
                        a.AssignedFromUtc <= now &&
                        (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.AssignedFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return assignment is null ? null : MapToDto(assignment);
    }

    public async Task<DriverVehicleAssignmentDto?> GetActiveAssignmentForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;

        var assignment = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId &&
                        a.VehicleId == vehicleId &&
                        a.IsActive &&
                        a.AssignedFromUtc <= now &&
                        (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.AssignedFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return assignment is null ? null : MapToDto(assignment);
    }

    public async Task<DriverVehicleAssignmentDto> CreateAssignmentAsync(
        CreateDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        // Eligibility validation
        var eligibility = await _eligibilityService.EvaluateAsync(request.DriverId, request.VehicleId, cancellationToken);
        if (!eligibility.IsEligible)
        {
            throw new InvalidOperationException($"Cannot assign driver: {string.Join(" ", eligibility.Reasons)}");
        }

        var startTime = request.AssignedFromUtc ?? DateTime.UtcNow;

        var assignment = new DriverVehicleAssignment(
            tenantId,
            request.DriverId,
            request.VehicleId,
            request.AssignmentType,
            startTime,
            request.AssignedToUtc,
            request.IsPrimary,
            userId,
            request.Reason,
            request.Notes);

        _dbContext.DriverVehicleAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DriverVehicleAssigned,
                "DriverVehicleAssignment",
                assignment.Id.ToString(),
                $"Assigned driver {eligibility.DriverName} to vehicle {eligibility.VehicleNumber} ({request.AssignmentType})"),
            cancellationToken);

        // Reload with navigations
        var created = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .FirstAsync(a => a.Id == assignment.Id, cancellationToken);

        return MapToDto(created);
    }

    public async Task<DriverVehicleAssignmentDto> EndAssignmentAsync(
        Guid assignmentId,
        EndDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var assignment = await _dbContext.DriverVehicleAssignments
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Assignment '{assignmentId}' was not found.");

        assignment.EndAssignment(userId, request.Reason, request.EndedAtUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DriverVehicleAssignmentEnded,
                "DriverVehicleAssignment",
                assignment.Id.ToString(),
                $"Ended assignment for driver {assignment.Driver?.DisplayName} and vehicle {assignment.Vehicle?.VehicleNumber}"),
            cancellationToken);

        return MapToDto(assignment);
    }

    private static DriverVehicleAssignmentDto MapToDto(DriverVehicleAssignment a) =>
        new(
            a.Id,
            a.TenantId,
            a.DriverId,
            a.Driver?.DriverNumber ?? string.Empty,
            a.Driver?.DisplayName ?? string.Empty,
            a.VehicleId,
            a.Vehicle?.VehicleNumber ?? string.Empty,
            a.Vehicle?.DisplayName ?? string.Empty,
            a.Vehicle?.RegistrationNumber,
            a.AssignmentType,
            a.AssignmentType.ToString(),
            a.AssignedFromUtc,
            a.AssignedToUtc,
            a.IsPrimary,
            a.IsActive,
            a.AssignedByUserId,
            a.EndedByUserId,
            a.Reason,
            a.Notes,
            a.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

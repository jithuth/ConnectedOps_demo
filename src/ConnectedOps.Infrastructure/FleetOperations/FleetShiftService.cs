using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetShiftService : IFleetShiftService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FleetShiftService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<FleetShiftDto>> GetShiftsAsync(
        Guid? branchId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FleetShifts
            .AsNoTracking()
            .Include(s => s.Branch)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var shifts = await query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return shifts.Select(MapToDto).ToList();
    }

    public async Task<FleetShiftDto> GetShiftByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shift = await _dbContext.FleetShifts
            .AsNoTracking()
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (shift is null)
            throw new KeyNotFoundException($"Fleet shift '{id}' was not found.");

        return MapToDto(shift);
    }

    public async Task<FleetShiftDto> CreateShiftAsync(
        CreateFleetShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shift = new FleetShift(
            tenantId,
            request.Name,
            request.Code,
            request.StartTime,
            request.EndTime,
            request.DaysOfWeek,
            request.BranchId,
            request.Description);

        _dbContext.FleetShifts.Add(shift);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftCreated,
                "FleetShift",
                shift.Id.ToString(),
                $"Created fleet shift '{shift.Name}' ({shift.Code})"),
            cancellationToken);

        return MapToDto(shift);
    }

    public async Task<FleetShiftDto> UpdateShiftAsync(
        Guid id,
        UpdateFleetShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shift = await _dbContext.FleetShifts
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (shift is null)
            throw new KeyNotFoundException($"Fleet shift '{id}' was not found.");

        shift.UpdateDetails(
            request.Name,
            request.Code,
            request.StartTime,
            request.EndTime,
            request.DaysOfWeek,
            request.BranchId,
            request.Description);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftUpdated,
                "FleetShift",
                shift.Id.ToString(),
                $"Updated fleet shift '{shift.Name}'"),
            cancellationToken);

        return MapToDto(shift);
    }

    public async Task ActivateShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shift = await _dbContext.FleetShifts
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (shift is null)
            throw new KeyNotFoundException($"Fleet shift '{id}' was not found.");

        shift.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftActivated,
                "FleetShift",
                shift.Id.ToString(),
                $"Activated fleet shift '{shift.Name}'"),
            cancellationToken);
    }

    public async Task DeactivateShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var shift = await _dbContext.FleetShifts
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (shift is null)
            throw new KeyNotFoundException($"Fleet shift '{id}' was not found.");

        shift.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftDeactivated,
                "FleetShift",
                shift.Id.ToString(),
                $"Deactivated fleet shift '{shift.Name}'"),
            cancellationToken);
    }

    public async Task DeleteShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var shift = await _dbContext.FleetShifts
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (shift is null)
            throw new KeyNotFoundException($"Fleet shift '{id}' was not found.");

        shift.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetShiftDeleted,
                "FleetShift",
                shift.Id.ToString(),
                $"Deleted fleet shift '{shift.Name}'"),
            cancellationToken);
    }

    private static FleetShiftDto MapToDto(FleetShift s) =>
        new(
            s.Id,
            s.TenantId,
            s.Name,
            s.Code,
            s.BranchId,
            s.Branch?.Name,
            s.StartTime,
            s.EndTime,
            s.CrossesMidnight,
            s.DaysOfWeek,
            s.Description,
            s.IsActive,
            s.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

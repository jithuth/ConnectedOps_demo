using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetOperationalExceptionService : IFleetOperationalExceptionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FleetOperationalExceptionService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<FleetOperationalExceptionDto>> GetExceptionsPagedAsync(
        OperationalExceptionQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FleetOperationalExceptions
            .AsNoTracking()
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .Where(e => e.TenantId == tenantId);

        if (parameters.VehicleId.HasValue)
            query = query.Where(e => e.VehicleId == parameters.VehicleId.Value);

        if (parameters.DriverId.HasValue)
            query = query.Where(e => e.DriverId == parameters.DriverId.Value);

        if (parameters.Type.HasValue)
            query = query.Where(e => e.ExceptionType == parameters.Type.Value);

        if (parameters.Severity.HasValue)
            query = query.Where(e => e.Severity == parameters.Severity.Value);

        if (parameters.Status.HasValue)
            query = query.Where(e => e.Status == parameters.Status.Value);

        if (parameters.FromUtc.HasValue)
            query = query.Where(e => e.OccurredAtUtc >= parameters.FromUtc.Value);

        if (parameters.ToUtc.HasValue)
            query = query.Where(e => e.OccurredAtUtc <= parameters.ToUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 20 : parameters.PageSize;

        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<FleetOperationalExceptionDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<FleetOperationalExceptionDto> GetExceptionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var exception = await _dbContext.FleetOperationalExceptions
            .AsNoTracking()
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (exception is null)
            throw new KeyNotFoundException($"Fleet operational exception '{id}' was not found.");

        return MapToDto(exception);
    }

    public async Task<FleetOperationalExceptionDto> CreateExceptionAsync(
        CreateOperationalExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var exception = new FleetOperationalException(
            tenantId,
            request.ExceptionType,
            request.Severity,
            request.Description,
            request.VehicleId,
            request.DriverId,
            request.UsageSessionId,
            request.OccurredAtUtc);

        _dbContext.FleetOperationalExceptions.Add(exception);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetOperationalExceptionCreated,
                "FleetOperationalException",
                exception.Id.ToString(),
                $"Reported {request.Severity} operational exception: {request.Description}"),
            cancellationToken);

        return await GetExceptionByIdAsync(exception.Id, cancellationToken);
    }

    public async Task<FleetOperationalExceptionDto> ResolveExceptionAsync(
        Guid id,
        ResolveOperationalExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId ?? Guid.Empty;

        var exception = await _dbContext.FleetOperationalExceptions
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (exception is null)
            throw new KeyNotFoundException($"Fleet operational exception '{id}' was not found.");

        exception.Resolve(userId, request.ResolutionNotes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetOperationalExceptionResolved,
                "FleetOperationalException",
                exception.Id.ToString(),
                $"Resolved operational exception {id}: {request.ResolutionNotes}"),
            cancellationToken);

        return MapToDto(exception);
    }

    public async Task<FleetOperationalExceptionDto> DismissExceptionAsync(
        Guid id,
        DismissOperationalExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId ?? Guid.Empty;

        var exception = await _dbContext.FleetOperationalExceptions
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (exception is null)
            throw new KeyNotFoundException($"Fleet operational exception '{id}' was not found.");

        exception.Dismiss(userId, request.DismissalNotes ?? "Dismissed");
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FleetOperationalExceptionDismissed,
                "FleetOperationalException",
                exception.Id.ToString(),
                $"Dismissed operational exception {id}"),
            cancellationToken);

        return MapToDto(exception);
    }

    private static FleetOperationalExceptionDto MapToDto(FleetOperationalException e) =>
        new(
            e.Id,
            e.TenantId,
            e.VehicleId,
            e.Vehicle?.VehicleNumber,
            e.Vehicle?.DisplayName,
            e.DriverId,
            e.Driver?.DriverNumber,
            e.Driver?.DisplayName,
            e.UsageSessionId,
            e.ExceptionType,
            e.ExceptionType.ToString(),
            e.Severity,
            e.Severity.ToString(),
            e.Description,
            e.OccurredAtUtc,
            e.ResolvedAtUtc,
            e.ResolvedByUserId,
            null,
            e.ResolutionNotes,
            e.Status,
            e.Status.ToString(),
            e.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

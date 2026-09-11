using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetActivityTimelineService : IFleetActivityTimelineService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public FleetActivityTimelineService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<PagedResult<FleetActivityItemDto>> GetActivityTimelinePagedAsync(
        FleetActivityQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var items = new List<FleetActivityItemDto>();

        // 1. Sessions (Checkouts and Checkins)
        var sessionsQuery = _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId);

        if (parameters.VehicleId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.VehicleId == parameters.VehicleId.Value);
        if (parameters.DriverId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.DriverId == parameters.DriverId.Value);
        if (parameters.BranchId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Vehicle.BranchId == parameters.BranchId.Value);
        if (parameters.FromUtc.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.CheckedOutAtUtc >= parameters.FromUtc.Value);
        if (parameters.ToUtc.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.CheckedOutAtUtc <= parameters.ToUtc.Value);

        var sessions = await sessionsQuery
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var s in sessions)
        {
            if (string.IsNullOrWhiteSpace(parameters.EventType) || parameters.EventType.Equals("Checkout", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new FleetActivityItemDto(
                    s.Id,
                    "Checkout",
                    "Vehicle Checked Out",
                    s.CheckedOutAtUtc,
                    s.VehicleId,
                    s.Vehicle?.VehicleNumber,
                    s.DriverId,
                    s.Driver?.DisplayName,
                    $"Vehicle '{s.Vehicle?.VehicleNumber}' checked out to driver '{s.Driver?.DisplayName}' at {s.StartOdometer} {s.OdometerUnit}. Condition: {s.CheckoutCondition}.",
                    "bg-primary",
                    "bi-box-arrow-right"));
            }

            if (s.Status == UsageSessionStatus.Completed && s.CheckedInAtUtc.HasValue)
            {
                if (string.IsNullOrWhiteSpace(parameters.EventType) || parameters.EventType.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new FleetActivityItemDto(
                        Guid.NewGuid(),
                        "CheckIn",
                        "Vehicle Checked In",
                        s.CheckedInAtUtc.Value,
                        s.VehicleId,
                        s.Vehicle?.VehicleNumber,
                        s.DriverId,
                        s.Driver?.DisplayName,
                        $"Vehicle '{s.Vehicle?.VehicleNumber}' checked in by '{s.Driver?.DisplayName}' at {s.EndOdometer} {s.OdometerUnit} (Distance: {s.DistanceTraveled:0.##} {s.OdometerUnit}). Condition: {s.CheckInCondition}.",
                        "bg-success",
                        "bi-box-arrow-in-left"));
                }
            }
        }

        // 2. Handovers
        if (string.IsNullOrWhiteSpace(parameters.EventType) || parameters.EventType.Equals("Handover", StringComparison.OrdinalIgnoreCase))
        {
            var handoversQuery = _dbContext.VehicleHandovers
                .AsNoTracking()
                .Include(h => h.Vehicle)
                .Include(h => h.FromDriver)
                .Include(h => h.ToDriver)
                .Where(h => h.TenantId == tenantId);

            if (parameters.VehicleId.HasValue)
                handoversQuery = handoversQuery.Where(h => h.VehicleId == parameters.VehicleId.Value);
            if (parameters.DriverId.HasValue)
                handoversQuery = handoversQuery.Where(h => h.FromDriverId == parameters.DriverId.Value || h.ToDriverId == parameters.DriverId.Value);
            if (parameters.BranchId.HasValue)
                handoversQuery = handoversQuery.Where(h => h.Vehicle.BranchId == parameters.BranchId.Value);
            if (parameters.FromUtc.HasValue)
                handoversQuery = handoversQuery.Where(h => h.HandoverAtUtc >= parameters.FromUtc.Value);
            if (parameters.ToUtc.HasValue)
                handoversQuery = handoversQuery.Where(h => h.HandoverAtUtc <= parameters.ToUtc.Value);

            var handovers = await handoversQuery
                .OrderByDescending(h => h.HandoverAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var h in handovers)
            {
                var fromName = h.FromDriver?.DisplayName ?? "Fleet Pool";
                items.Add(new FleetActivityItemDto(
                    h.Id,
                    "Handover",
                    "Custody Handover",
                    h.HandoverAtUtc,
                    h.VehicleId,
                    h.Vehicle?.VehicleNumber,
                    h.ToDriverId,
                    h.ToDriver?.DisplayName,
                    $"Vehicle '{h.Vehicle?.VehicleNumber}' transferred from {fromName} to {h.ToDriver?.DisplayName} at {h.Odometer} {h.OdometerUnit}.",
                    "bg-warning text-dark",
                    "bi-arrow-left-right"));
            }
        }

        // 3. Operational Exceptions
        if (string.IsNullOrWhiteSpace(parameters.EventType) || parameters.EventType.Equals("Exception", StringComparison.OrdinalIgnoreCase))
        {
            var exceptionsQuery = _dbContext.FleetOperationalExceptions
                .AsNoTracking()
                .Include(e => e.Vehicle)
                .Include(e => e.Driver)
                .Where(e => e.TenantId == tenantId);

            if (parameters.VehicleId.HasValue)
                exceptionsQuery = exceptionsQuery.Where(e => e.VehicleId == parameters.VehicleId.Value);
            if (parameters.DriverId.HasValue)
                exceptionsQuery = exceptionsQuery.Where(e => e.DriverId == parameters.DriverId.Value);
            if (parameters.FromUtc.HasValue)
                exceptionsQuery = exceptionsQuery.Where(e => e.OccurredAtUtc >= parameters.FromUtc.Value);
            if (parameters.ToUtc.HasValue)
                exceptionsQuery = exceptionsQuery.Where(e => e.OccurredAtUtc <= parameters.ToUtc.Value);

            var exceptions = await exceptionsQuery
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var e in exceptions)
            {
                items.Add(new FleetActivityItemDto(
                    e.Id,
                    "Exception",
                    $"Exception: {e.ExceptionType}",
                    e.OccurredAtUtc,
                    e.VehicleId,
                    e.Vehicle?.VehicleNumber,
                    e.DriverId,
                    e.Driver?.DisplayName,
                    $"{e.Severity} severity: {e.Description}",
                    e.Severity == OperationalExceptionSeverity.Critical ? "bg-danger" : "bg-warning text-dark",
                    "bi-exclamation-triangle"));
            }
        }

        // Sort all items descending by timestamp
        var sorted = items.OrderByDescending(i => i.TimestampUtc).ToList();
        var totalCount = sorted.Count;

        var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 20 : parameters.PageSize;

        var pagedItems = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FleetActivityItemDto>(pagedItems, totalCount, pageNumber, pageSize);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

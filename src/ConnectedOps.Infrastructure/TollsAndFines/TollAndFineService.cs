using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.TollsAndFines;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.TollsAndFines;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.TollsAndFines;

public sealed class TollAndFineService : ITollAndFineService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TollAndFineService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        if (!_currentUserContext.TenantId.HasValue || _currentUserContext.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return _currentUserContext.TenantId.Value;
    }

    public async Task<TollsAndFinesDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var tolls = await _dbContext.TollTransactions
            .Include(t => t.Vehicle)
            .Include(t => t.MatchedDriver)
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var violations = await _dbContext.TrafficViolations
            .Include(v => v.Vehicle)
            .Include(v => v.MatchedDriver)
            .Where(v => v.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var monthlyTolls = tolls.Where(t => t.TransactionTimeUtc >= startOfMonth).ToList();
        var totalTollAmount = monthlyTolls.Sum(t => t.Amount);
        var totalTollCount = monthlyTolls.Count;

        var pendingFines = violations.Where(v => v.LiabilityStatus == ViolationLiabilityStatus.PendingReview || v.LiabilityStatus == ViolationLiabilityStatus.AssignedToDriver).ToList();
        var disputedFines = violations.Where(v => v.LiabilityStatus == ViolationLiabilityStatus.Disputed).ToList();

        return new TollsAndFinesDashboardDto(
            TotalTollsAmountThisMonth: totalTollAmount,
            TotalTollsCountThisMonth: totalTollCount,
            TotalFinesAmountPending: pendingFines.Sum(f => f.FineAmount),
            PendingFinesCount: pendingFines.Count,
            DisputedFinesCount: disputedFines.Count,
            RecentTolls: tolls.OrderByDescending(t => t.TransactionTimeUtc).Take(10).Select(MapToTollDto).ToList(),
            RecentViolations: violations.OrderByDescending(v => v.ViolationTimeUtc).Take(10).Select(MapToViolationDto).ToList());
    }

    public async Task<PagedResult<TollTransactionDto>> GetTollsPagedAsync(TollFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.TollTransactions
            .Include(t => t.Vehicle)
            .Include(t => t.MatchedDriver)
            .Where(t => t.TenantId == tenantId);

        if (request.TollSystem.HasValue)
            query = query.Where(t => t.TollSystem == request.TollSystem.Value);

        if (request.VehicleId.HasValue)
            query = query.Where(t => t.VehicleId == request.VehicleId.Value);

        if (request.DriverId.HasValue)
            query = query.Where(t => t.MatchedDriverId == request.DriverId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.TollGateName.ToLower().Contains(term) ||
                t.TollGateCode.ToLower().Contains(term) ||
                (t.Vehicle.RegistrationNumber != null && t.Vehicle.RegistrationNumber.ToLower().Contains(term)) ||
                t.Vehicle.VehicleNumber.ToLower().Contains(term) ||
                (t.TagNumber != null && t.TagNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionTimeUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TollTransactionDto>(
            items.Select(MapToTollDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<PagedResult<TrafficViolationDto>> GetViolationsPagedAsync(ViolationFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.TrafficViolations
            .Include(v => v.Vehicle)
            .Include(v => v.MatchedDriver)
            .Where(v => v.TenantId == tenantId);

        if (request.LiabilityStatus.HasValue)
            query = query.Where(v => v.LiabilityStatus == request.LiabilityStatus.Value);

        if (request.VehicleId.HasValue)
            query = query.Where(v => v.VehicleId == request.VehicleId.Value);

        if (request.DriverId.HasValue)
            query = query.Where(v => v.MatchedDriverId == request.DriverId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(v =>
                v.TicketNumber.ToLower().Contains(term) ||
                v.AuthorityName.ToLower().Contains(term) ||
                v.ViolationCode.ToLower().Contains(term) ||
                v.Description.ToLower().Contains(term) ||
                (v.Vehicle.RegistrationNumber != null && v.Vehicle.RegistrationNumber.ToLower().Contains(term)) ||
                v.Vehicle.VehicleNumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.ViolationTimeUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TrafficViolationDto>(
            items.Select(MapToViolationDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<TollTransactionDto> CreateTollAsync(CreateTollTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);
        if (vehicle == null)
            throw new KeyNotFoundException($"Vehicle with ID {request.VehicleId} was not found.");

        var toll = new TollTransaction(
            tenantId,
            request.TollSystem,
            request.TollGateName,
            request.TollGateCode,
            request.Amount,
            request.TransactionTimeUtc,
            request.VehicleId,
            request.TagNumber);

        // Check if there was an active usage session during this transaction time
        var session = await _dbContext.VehicleUsageSessions
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId && s.VehicleId == request.VehicleId
                && s.CheckedOutAtUtc <= request.TransactionTimeUtc
                && (s.CheckedInAtUtc == null || s.CheckedInAtUtc >= request.TransactionTimeUtc))
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session != null)
        {
            toll.AssignMatchedDriver(session.DriverId, session.Id, _currentUserContext.UserId);
            toll.MatchedDriver = session.Driver;
        }

        toll.Vehicle = vehicle;
        await _dbContext.TollTransactions.AddAsync(toll, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToTollDto(toll);
    }

    public async Task<TrafficViolationDto> CreateViolationAsync(CreateTrafficViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);
        if (vehicle == null)
            throw new KeyNotFoundException($"Vehicle with ID {request.VehicleId} was not found.");

        var violation = new TrafficViolation(
            tenantId,
            request.TicketNumber,
            request.AuthorityName,
            request.ViolationCode,
            request.Description,
            request.FineAmount,
            request.BlackPoints,
            request.ViolationTimeUtc,
            request.VehicleId,
            request.Location);

        // Check if there was an active usage session during this violation time
        var session = await _dbContext.VehicleUsageSessions
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId && s.VehicleId == request.VehicleId
                && s.CheckedOutAtUtc <= request.ViolationTimeUtc
                && (s.CheckedInAtUtc == null || s.CheckedInAtUtc >= request.ViolationTimeUtc))
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session != null)
        {
            violation.AssignDriver(session.DriverId, session.Id, _currentUserContext.UserId);
            violation.MatchedDriver = session.Driver;
        }

        violation.Vehicle = vehicle;
        await _dbContext.TrafficViolations.AddAsync(violation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToViolationDto(violation);
    }

    public async Task<TrafficViolationDto> AssignLiabilityAsync(Guid violationId, AssignViolationLiabilityRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var violation = await _dbContext.TrafficViolations
            .Include(v => v.Vehicle)
            .Include(v => v.MatchedDriver)
            .FirstOrDefaultAsync(v => v.Id == violationId && v.TenantId == tenantId, cancellationToken);

        if (violation == null)
            throw new KeyNotFoundException($"Traffic violation with ID {violationId} was not found.");

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.DriverId && d.TenantId == tenantId, cancellationToken);
        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID {request.DriverId} was not found.");

        violation.AssignDriver(request.DriverId, request.UsageSessionId, _currentUserContext.UserId);
        violation.MatchedDriver = driver;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToViolationDto(violation);
    }

    public async Task<TrafficViolationDto> DisputeViolationAsync(Guid violationId, DisputeViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var violation = await _dbContext.TrafficViolations
            .Include(v => v.Vehicle)
            .Include(v => v.MatchedDriver)
            .FirstOrDefaultAsync(v => v.Id == violationId && v.TenantId == tenantId, cancellationToken);

        if (violation == null)
            throw new KeyNotFoundException($"Traffic violation with ID {violationId} was not found.");

        violation.Dispute(request.DisputeReason, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToViolationDto(violation);
    }

    public async Task<TrafficViolationDto> SettleViolationAsync(Guid violationId, SettleViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var violation = await _dbContext.TrafficViolations
            .Include(v => v.Vehicle)
            .Include(v => v.MatchedDriver)
            .FirstOrDefaultAsync(v => v.Id == violationId && v.TenantId == tenantId, cancellationToken);

        if (violation == null)
            throw new KeyNotFoundException($"Traffic violation with ID {violationId} was not found.");

        violation.Settle(request.Status, request.Notes, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToViolationDto(violation);
    }

    public async Task<int> AutoMatchDriverLiabilityAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var matchedCount = 0;

        var unmatchedTolls = await _dbContext.TollTransactions
            .Where(t => t.TenantId == tenantId && t.MatchedDriverId == null)
            .ToListAsync(cancellationToken);

        foreach (var toll in unmatchedTolls)
        {
            var session = await _dbContext.VehicleUsageSessions
                .Where(s => s.TenantId == tenantId && s.VehicleId == toll.VehicleId
                    && s.CheckedOutAtUtc <= toll.TransactionTimeUtc
                    && (s.CheckedInAtUtc == null || s.CheckedInAtUtc >= toll.TransactionTimeUtc))
                .OrderByDescending(s => s.CheckedOutAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (session != null)
            {
                toll.AssignMatchedDriver(session.DriverId, session.Id, _currentUserContext.UserId);
                matchedCount++;
            }
        }

        var unmatchedViolations = await _dbContext.TrafficViolations
            .Where(v => v.TenantId == tenantId && v.MatchedDriverId == null)
            .ToListAsync(cancellationToken);

        foreach (var violation in unmatchedViolations)
        {
            var session = await _dbContext.VehicleUsageSessions
                .Where(s => s.TenantId == tenantId && s.VehicleId == violation.VehicleId
                    && s.CheckedOutAtUtc <= violation.ViolationTimeUtc
                    && (s.CheckedInAtUtc == null || s.CheckedInAtUtc >= violation.ViolationTimeUtc))
                .OrderByDescending(s => s.CheckedOutAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (session != null)
            {
                violation.AssignDriver(session.DriverId, session.Id, _currentUserContext.UserId);
                matchedCount++;
            }
        }

        if (matchedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return matchedCount;
    }

    private static TollTransactionDto MapToTollDto(TollTransaction t) =>
        new(
            Id: t.Id,
            TollSystem: t.TollSystem,
            TollSystemName: t.TollSystem.ToString(),
            TollGateName: t.TollGateName,
            TollGateCode: t.TollGateCode,
            Amount: t.Amount,
            TransactionTimeUtc: t.TransactionTimeUtc,
            VehicleId: t.VehicleId,
            VehiclePlateNumber: t.Vehicle != null ? (t.Vehicle.RegistrationNumber ?? t.Vehicle.VehicleNumber) : "N/A",
            TagNumber: t.TagNumber,
            MatchedDriverId: t.MatchedDriverId,
            MatchedDriverName: t.MatchedDriver != null ? $"{t.MatchedDriver.FirstName} {t.MatchedDriver.LastName}".Trim() : null,
            MatchedUsageSessionId: t.MatchedUsageSessionId);

    private static TrafficViolationDto MapToViolationDto(TrafficViolation v) =>
        new(
            Id: v.Id,
            TicketNumber: v.TicketNumber,
            AuthorityName: v.AuthorityName,
            ViolationCode: v.ViolationCode,
            Description: v.Description,
            FineAmount: v.FineAmount,
            BlackPoints: v.BlackPoints,
            ViolationTimeUtc: v.ViolationTimeUtc,
            Location: v.Location,
            VehicleId: v.VehicleId,
            VehiclePlateNumber: v.Vehicle != null ? (v.Vehicle.RegistrationNumber ?? v.Vehicle.VehicleNumber) : "N/A",
            MatchedDriverId: v.MatchedDriverId,
            MatchedDriverName: v.MatchedDriver != null ? $"{v.MatchedDriver.FirstName} {v.MatchedDriver.LastName}".Trim() : null,
            MatchedUsageSessionId: v.MatchedUsageSessionId,
            LiabilityStatus: v.LiabilityStatus,
            LiabilityStatusName: v.LiabilityStatus.ToString(),
            DisputeReason: v.DisputeReason,
            ResolutionNotes: v.ResolutionNotes,
            SettledAtUtc: v.SettledAtUtc);
}

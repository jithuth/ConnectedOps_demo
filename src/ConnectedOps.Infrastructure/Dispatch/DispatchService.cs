using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Dispatch;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Dispatch;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Dispatch;

public sealed class DispatchService : IDispatchService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DispatchService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        if (!_currentUserContext.TenantId.HasValue || _currentUserContext.TenantId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is required for dispatch operations.");
        }
        return _currentUserContext.TenantId.Value;
    }

    // =========================================================================
    // DASHBOARD
    // =========================================================================
    public async Task<DispatchDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStartUtc = DateTime.UtcNow.Date;
        var todayEndUtc = todayStartUtc.AddDays(1);

        var jobsQuery = _dbContext.DispatchJobs
            .Where(j => j.TenantId == tenantId &&
                       ((j.CreatedAtUtc >= todayStartUtc && j.CreatedAtUtc < todayEndUtc) ||
                        (j.TimeWindowStartUtc.HasValue && j.TimeWindowStartUtc.Value >= todayStartUtc && j.TimeWindowStartUtc.Value < todayEndUtc)));

        var totalJobsToday = await jobsQuery.CountAsync(cancellationToken);
        var completedJobsToday = await jobsQuery.CountAsync(j => j.Status == DispatchJobStatus.Completed, cancellationToken);
        var inProgressJobsToday = await jobsQuery.CountAsync(j => j.Status == DispatchJobStatus.InProgress, cancellationToken);
        var pendingJobsToday = await jobsQuery.CountAsync(j => j.Status == DispatchJobStatus.Unassigned || j.Status == DispatchJobStatus.Scheduled || j.Status == DispatchJobStatus.Dispatched, cancellationToken);
        var failedJobsToday = await jobsQuery.CountAsync(j => j.Status == DispatchJobStatus.Failed, cancellationToken);

        // On-Time Delivery Rate
        var completedJobs = await jobsQuery
            .Where(j => j.Status == DispatchJobStatus.Completed && j.CompletedAtUtc.HasValue && j.TimeWindowEndUtc.HasValue)
            .ToListAsync(cancellationToken);

        decimal onTimeRate = 100m;
        if (completedJobs.Count > 0)
        {
            var onTimeCount = completedJobs.Count(j => j.CompletedAtUtc!.Value <= j.TimeWindowEndUtc!.Value.AddMinutes(15));
            onTimeRate = Math.Round((decimal)onTimeCount / completedJobs.Count * 100m, 1);
        }

        // Routes
        var routesQuery = _dbContext.DispatchRoutes
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .Where(r => r.TenantId == tenantId && r.ScheduledDate == today);

        var totalRoutes = await routesQuery.CountAsync(cancellationToken);
        var activeRoutes = await routesQuery.CountAsync(r => r.Status == DispatchRouteStatus.Dispatched || r.Status == DispatchRouteStatus.InProgress, cancellationToken);
        var completedRoutes = await routesQuery.CountAsync(r => r.Status == DispatchRouteStatus.Completed, cancellationToken);
        var totalDistanceKmToday = await routesQuery.SumAsync(r => r.ActualDistanceKm ?? r.EstimatedDistanceKm, cancellationToken);

        var activeRoutesList = await routesQuery
            .Where(r => r.Status == DispatchRouteStatus.Dispatched || r.Status == DispatchRouteStatus.InProgress)
            .OrderBy(r => r.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentCriticalJobs = await _dbContext.DispatchJobs
            .Where(j => j.TenantId == tenantId && (j.Priority == DispatchJobPriority.High || j.Priority == DispatchJobPriority.Urgent || j.Status == DispatchJobStatus.Failed))
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(8)
            .ToListAsync(cancellationToken);

        return new DispatchDashboardDto
        {
            TotalJobsToday = totalJobsToday,
            CompletedJobsToday = completedJobsToday,
            InProgressJobsToday = inProgressJobsToday,
            PendingJobsToday = pendingJobsToday,
            FailedJobsToday = failedJobsToday,
            OnTimeDeliveryRate = onTimeRate,
            TotalRoutesScheduled = totalRoutes,
            ActiveRoutes = activeRoutes,
            CompletedRoutes = completedRoutes,
            TotalDistanceKmToday = totalDistanceKmToday,
            ActiveRoutesList = activeRoutesList.Select(MapToRouteDto).ToList(),
            RecentCriticalJobs = recentCriticalJobs.Select(MapToJobDto).ToList()
        };
    }

    // =========================================================================
    // JOBS
    // =========================================================================
    public async Task<PagedResult<DispatchJobDto>> GetJobsPagedAsync(
        DispatchJobFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.DispatchJobs
            .Include(j => j.AssignedRoute)
            .Include(j => j.AssignedVehicle)
            .Include(j => j.AssignedDriver)
            .Where(j => j.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(j => j.JobNumber.ToLower().Contains(search) ||
                                     j.Title.ToLower().Contains(search) ||
                                     j.CustomerName.ToLower().Contains(search) ||
                                     j.Address.ToLower().Contains(search));
        }

        if (filter.Status.HasValue)
            query = query.Where(j => j.Status == filter.Status.Value);
        if (filter.JobType.HasValue)
            query = query.Where(j => j.JobType == filter.JobType.Value);
        if (filter.Priority.HasValue)
            query = query.Where(j => j.Priority == filter.Priority.Value);
        if (filter.AssignedVehicleId.HasValue)
            query = query.Where(j => j.AssignedVehicleId == filter.AssignedVehicleId.Value);
        if (filter.AssignedDriverId.HasValue)
            query = query.Where(j => j.AssignedDriverId == filter.AssignedDriverId.Value);
        if (filter.AssignedRouteId.HasValue)
            query = query.Where(j => j.AssignedRouteId == filter.AssignedRouteId.Value);
        if (filter.FromDateUtc.HasValue)
            query = query.Where(j => j.CreatedAtUtc >= filter.FromDateUtc.Value);
        if (filter.ToDateUtc.HasValue)
            query = query.Where(j => j.CreatedAtUtc <= filter.ToDateUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DispatchJobDto>(
            items.Select(MapToJobDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<DispatchJobDto?> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var job = await _dbContext.DispatchJobs
            .Include(j => j.AssignedRoute)
            .Include(j => j.AssignedVehicle)
            .Include(j => j.AssignedDriver)
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, cancellationToken);

        return job == null ? null : MapToJobDto(job);
    }

    public async Task<DispatchJobDto> CreateJobAsync(CreateDispatchJobRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var jobNumber = $"JOB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        var job = new DispatchJob(
            tenantId: tenantId,
            jobNumber: jobNumber,
            title: request.Title,
            jobType: request.JobType,
            priority: request.Priority,
            customerName: request.CustomerName,
            customerPhone: request.CustomerPhone,
            address: request.Address,
            latitude: request.Latitude,
            longitude: request.Longitude,
            timeWindowStartUtc: request.TimeWindowStartUtc,
            timeWindowEndUtc: request.TimeWindowEndUtc,
            serviceDurationMinutes: request.ServiceDurationMinutes,
            weightKg: request.WeightKg,
            volumeM3: request.VolumeM3,
            packageCount: request.PackageCount,
            specialInstructions: request.SpecialInstructions,
            customerEmail: request.CustomerEmail,
            createdByUserId: _currentUserContext.UserId);

        _dbContext.DispatchJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToJobDto(job);
    }

    public async Task<DispatchJobDto> UpdateJobAsync(Guid id, UpdateDispatchJobRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var job = await _dbContext.DispatchJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, cancellationToken);

        if (job == null)
            throw new KeyNotFoundException($"Dispatch job with ID '{id}' was not found.");

        job.Update(
            title: request.Title,
            jobType: request.JobType,
            priority: request.Priority,
            customerName: request.CustomerName,
            customerPhone: request.CustomerPhone,
            customerEmail: request.CustomerEmail,
            address: request.Address,
            latitude: request.Latitude,
            longitude: request.Longitude,
            timeWindowStartUtc: request.TimeWindowStartUtc,
            timeWindowEndUtc: request.TimeWindowEndUtc,
            serviceDurationMinutes: request.ServiceDurationMinutes,
            weightKg: request.WeightKg,
            volumeM3: request.VolumeM3,
            packageCount: request.PackageCount,
            specialInstructions: request.SpecialInstructions,
            updatedBy: _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToJobDto(job);
    }

    public async Task<bool> CancelJobAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var job = await _dbContext.DispatchJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, cancellationToken);

        if (job == null)
            return false;

        job.Cancel(reason, _currentUserContext.UserId);

        // Remove from route stop if assigned
        var stop = await _dbContext.DispatchRouteStops
            .FirstOrDefaultAsync(s => s.JobId == id && s.TenantId == tenantId, cancellationToken);

        if (stop != null)
        {
            stop.MarkSkipped(reason);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // =========================================================================
    // ROUTES
    // =========================================================================
    public async Task<PagedResult<DispatchRouteDto>> GetRoutesPagedAsync(
        DispatchRouteFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.DispatchRoutes
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.StartLocation)
            .Include(r => r.EndLocation)
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.RouteNumber.ToLower().Contains(search) ||
                                     r.Name.ToLower().Contains(search));
        }

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.ScheduledDate.HasValue)
            query = query.Where(r => r.ScheduledDate == filter.ScheduledDate.Value);
        if (filter.VehicleId.HasValue)
            query = query.Where(r => r.VehicleId == filter.VehicleId.Value);
        if (filter.DriverId.HasValue)
            query = query.Where(r => r.DriverId == filter.DriverId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(r => r.ScheduledDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DispatchRouteDto>(
            items.Select(MapToRouteDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<DispatchRouteDto?> GetRouteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.StartLocation)
            .Include(r => r.EndLocation)
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .Include(r => r.Stops)
                .ThenInclude(s => s.ProofOfDelivery)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        return route == null ? null : MapToRouteDto(route);
    }

    public async Task<DispatchRouteDto> CreateRouteAsync(CreateDispatchRouteRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var routeNumber = $"RT-{request.ScheduledDate:yyyyMMdd}-{Guid.NewGuid():N}"[..17].ToUpperInvariant();

        var route = new DispatchRoute(
            tenantId: tenantId,
            routeNumber: routeNumber,
            name: request.Name,
            scheduledDate: request.ScheduledDate,
            vehicleId: request.VehicleId,
            driverId: request.DriverId,
            startLocationId: request.StartLocationId,
            endLocationId: request.EndLocationId,
            notes: request.Notes,
            createdByUserId: _currentUserContext.UserId);

        _dbContext.DispatchRoutes.Add(route);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Add stops if jobs were provided
        if (request.JobIdsInSequence.Count > 0)
        {
            var jobs = await _dbContext.DispatchJobs
                .Where(j => j.TenantId == tenantId && request.JobIdsInSequence.Contains(j.Id))
                .ToDictionaryAsync(j => j.Id, cancellationToken);

            int seq = 1;
            foreach (var jobId in request.JobIdsInSequence)
            {
                if (jobs.TryGetValue(jobId, out var job))
                {
                    var stop = new DispatchRouteStop(
                        tenantId: tenantId,
                        routeId: route.Id,
                        jobId: jobId,
                        sequenceOrder: seq++,
                        createdByUserId: _currentUserContext.UserId);

                    route.AddStop(stop);
                    job.AssignToRoute(route.Id, request.VehicleId, request.DriverId, _currentUserContext.UserId);
                }
            }

            RecalculateRouteEstimates(route);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (await GetRouteByIdAsync(route.Id, cancellationToken))!;
    }

    public async Task<DispatchRouteDto> UpdateRouteAsync(Guid id, UpdateDispatchRouteRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            throw new KeyNotFoundException($"Dispatch route with ID '{id}' was not found.");

        route.UpdateGeneral(
            name: request.Name,
            scheduledDate: request.ScheduledDate,
            vehicleId: request.VehicleId,
            driverId: request.DriverId,
            startLocationId: request.StartLocationId,
            endLocationId: request.EndLocationId,
            notes: request.Notes,
            updatedBy: _currentUserContext.UserId);

        // Update assigned vehicle/driver on stops' jobs
        foreach (var stop in route.Stops)
        {
            stop.Job.AssignToRoute(route.Id, request.VehicleId, request.DriverId, _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetRouteByIdAsync(route.Id, cancellationToken))!;
    }

    public async Task<bool> DispatchRouteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            return false;

        route.Dispatch(_currentUserContext.UserId);

        var first = true;
        foreach (var stop in route.Stops)
        {
            if (first)
            {
                stop.MarkEnRoute(_currentUserContext.UserId);
                stop.Job.SetStatus(DispatchJobStatus.InProgress, _currentUserContext.UserId);
                first = false;
            }
            else
            {
                stop.Job.SetStatus(DispatchJobStatus.Dispatched, _currentUserContext.UserId);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> StartRouteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            return false;

        route.StartRoute(DateTime.UtcNow, _currentUserContext.UserId);

        var firstPending = route.Stops.FirstOrDefault(s => s.Status == RouteStopStatus.Pending);
        if (firstPending != null)
        {
            firstPending.MarkEnRoute(_currentUserContext.UserId);
            firstPending.Job.SetStatus(DispatchJobStatus.InProgress, _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CompleteRouteAsync(Guid id, decimal? actualDistanceKm = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            return false;

        route.CompleteRoute(actualDistanceKm ?? route.EstimatedDistanceKm, DateTime.UtcNow, _currentUserContext.UserId);

        // Mark remaining pending stops as completed
        foreach (var stop in route.Stops.Where(s => s.Status == RouteStopStatus.Pending || s.Status == RouteStopStatus.EnRoute || s.Status == RouteStopStatus.Arrived))
        {
            stop.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);
            stop.Job.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DispatchRouteDto> AddStopAsync(Guid routeId, AddRouteStopRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            throw new KeyNotFoundException($"Route with ID '{routeId}' was not found.");

        var job = await _dbContext.DispatchJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId && j.TenantId == tenantId, cancellationToken);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID '{request.JobId}' was not found.");

        var sequence = request.SequenceOrder > 0 ? request.SequenceOrder : route.Stops.Count + 1;

        var stop = new DispatchRouteStop(
            tenantId: tenantId,
            routeId: routeId,
            jobId: request.JobId,
            sequenceOrder: sequence,
            plannedArrivalUtc: request.PlannedArrivalUtc,
            notes: request.Notes,
            createdByUserId: _currentUserContext.UserId)
        {
            Job = job,
            Route = route
        };

        _dbContext.DispatchRouteStops.Add(stop);
        route.AddStop(stop);
        job.AssignToRoute(route.Id, route.VehicleId, route.DriverId, _currentUserContext.UserId);

        RecalculateRouteEstimates(route);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRouteByIdAsync(routeId, cancellationToken))!;
    }

    public async Task<DispatchRouteDto> RemoveStopAsync(Guid routeId, Guid stopId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            throw new KeyNotFoundException($"Route with ID '{routeId}' was not found.");

        var stop = route.Stops.FirstOrDefault(s => s.Id == stopId);
        if (stop != null)
        {
            stop.Job.Unassign(_currentUserContext.UserId);
            route.RemoveStop(stopId);
            _dbContext.DispatchRouteStops.Remove(stop);

            RecalculateRouteEstimates(route);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (await GetRouteByIdAsync(routeId, cancellationToken))!;
    }

    public async Task<DispatchRouteDto> ReorderStopsAsync(Guid routeId, List<Guid> stopIdsInOrder, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            throw new KeyNotFoundException($"Route with ID '{routeId}' was not found.");

        for (int i = 0; i < stopIdsInOrder.Count; i++)
        {
            var stopId = stopIdsInOrder[i];
            var stop = route.Stops.FirstOrDefault(s => s.Id == stopId);
            if (stop != null)
            {
                stop.UpdateSequence(i + 1, _currentUserContext.UserId);
            }
        }

        RecalculateRouteEstimates(route);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRouteByIdAsync(routeId, cancellationToken))!;
    }

    public async Task<DispatchRouteDto> OptimizeRouteAsync(Guid routeId, OptimizeRouteStopsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var route = await _dbContext.DispatchRoutes
            .Include(r => r.Stops)
                .ThenInclude(s => s.Job)
            .Include(r => r.StartLocation)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.TenantId == tenantId, cancellationToken);

        if (route == null)
            throw new KeyNotFoundException($"Route with ID '{routeId}' was not found.");

        var stops = route.Stops.ToList();
        if (stops.Count <= 2)
        {
            return MapToRouteDto(route);
        }

        // Determine depot starting point
        double startLat = request?.DepotLatitude ?? (route.StartLocation != null ? 0.0 : stops[0].Job.Latitude);
        double startLon = request?.DepotLongitude ?? (route.StartLocation != null ? 0.0 : stops[0].Job.Longitude);

        // Nearest-neighbor TSP heuristic
        var unvisited = new List<DispatchRouteStop>(stops);
        var ordered = new List<DispatchRouteStop>();

        var currentLat = startLat;
        var currentLon = startLon;

        while (unvisited.Count > 0)
        {
            DispatchRouteStop? bestStop = null;
            double bestDist = double.MaxValue;

            foreach (var stop in unvisited)
            {
                var dist = CalculateHaversineDistanceKm(currentLat, currentLon, stop.Job.Latitude, stop.Job.Longitude);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestStop = stop;
                }
            }

            if (bestStop != null)
            {
                ordered.Add(bestStop);
                unvisited.Remove(bestStop);
                currentLat = bestStop.Job.Latitude;
                currentLon = bestStop.Job.Longitude;
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].UpdateSequence(i + 1, _currentUserContext.UserId);
        }

        RecalculateRouteEstimates(route);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRouteByIdAsync(routeId, cancellationToken))!;
    }

    // =========================================================================
    // STOPS & PROOF OF DELIVERY
    // =========================================================================
    public async Task<DispatchRouteStopDto> UpdateStopStatusAsync(Guid stopId, UpdateRouteStopStatusRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var stop = await _dbContext.DispatchRouteStops
            .Include(s => s.Job)
            .Include(s => s.Route)
                .ThenInclude(r => r.Stops)
            .FirstOrDefaultAsync(s => s.Id == stopId && s.TenantId == tenantId, cancellationToken);

        if (stop == null)
            throw new KeyNotFoundException($"Route stop with ID '{stopId}' was not found.");

        switch (request.Status)
        {
            case RouteStopStatus.EnRoute:
                stop.MarkEnRoute(_currentUserContext.UserId);
                stop.Job.SetStatus(DispatchJobStatus.InProgress, _currentUserContext.UserId);
                break;
            case RouteStopStatus.Arrived:
                stop.MarkArrived(DateTime.UtcNow, _currentUserContext.UserId);
                break;
            case RouteStopStatus.Completed:
                stop.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);
                stop.Job.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);

                // Auto advance next stop to EnRoute
                var nextStop = stop.Route.Stops
                    .Where(s => s.SequenceOrder > stop.SequenceOrder && s.Status == RouteStopStatus.Pending)
                    .OrderBy(s => s.SequenceOrder)
                    .FirstOrDefault();

                if (nextStop != null)
                {
                    nextStop.MarkEnRoute(_currentUserContext.UserId);
                    nextStop.Job.SetStatus(DispatchJobStatus.InProgress, _currentUserContext.UserId);
                }
                break;
            case RouteStopStatus.Failed:
                stop.MarkFailed(request.ReasonOrNotes ?? "Stop delivery failed", _currentUserContext.UserId);
                stop.Job.MarkFailed(request.ReasonOrNotes ?? "Delivery failed", _currentUserContext.UserId);
                break;
            case RouteStopStatus.Skipped:
                stop.MarkSkipped(request.ReasonOrNotes, _currentUserContext.UserId);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToRouteStopDto(stop);
    }

    public async Task<ProofOfDeliveryDto> RecordProofOfDeliveryAsync(Guid jobId, RecordProofOfDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var job = await _dbContext.DispatchJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId, cancellationToken);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID '{jobId}' was not found.");

        var stop = await _dbContext.DispatchRouteStops
            .Include(s => s.Route)
                .ThenInclude(r => r.Stops)
            .FirstOrDefaultAsync(s => s.JobId == jobId && s.TenantId == tenantId, cancellationToken);

        var pod = new ProofOfDelivery(
            tenantId: tenantId,
            jobId: jobId,
            routeStopId: stop?.Id,
            verificationType: request.VerificationType,
            recipientName: request.RecipientName,
            signatureData: request.SignatureData,
            notes: request.Notes,
            latitude: request.Latitude,
            longitude: request.Longitude,
            completedAtUtc: DateTime.UtcNow,
            verifiedByUserId: _currentUserContext.UserId);

        _dbContext.ProofOfDeliveries.Add(pod);

        job.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);

        if (stop != null)
        {
            stop.ProofOfDelivery = pod;
            stop.MarkCompleted(DateTime.UtcNow, _currentUserContext.UserId);

            // Auto-advance next pending stop to EnRoute
            var nextStop = stop.Route.Stops
                .Where(s => s.SequenceOrder > stop.SequenceOrder && s.Status == RouteStopStatus.Pending)
                .OrderBy(s => s.SequenceOrder)
                .FirstOrDefault();

            if (nextStop != null)
            {
                nextStop.MarkEnRoute(_currentUserContext.UserId);
                nextStop.Job.SetStatus(DispatchJobStatus.InProgress, _currentUserContext.UserId);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProofOfDeliveryDto
        {
            Id = pod.Id,
            JobId = pod.JobId,
            JobNumber = job.JobNumber,
            RouteStopId = pod.RouteStopId,
            VerificationType = pod.VerificationType,
            RecipientName = pod.RecipientName,
            SignatureData = pod.SignatureData,
            Notes = pod.Notes,
            Latitude = pod.Latitude,
            Longitude = pod.Longitude,
            CompletedAtUtc = pod.CompletedAtUtc,
            VerifiedByUserId = pod.VerifiedByUserId
        };
    }

    public async Task<ProofOfDeliveryDto?> GetProofOfDeliveryByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var pod = await _dbContext.ProofOfDeliveries
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.JobId == jobId && p.TenantId == tenantId, cancellationToken);

        if (pod == null)
            return null;

        return new ProofOfDeliveryDto
        {
            Id = pod.Id,
            JobId = pod.JobId,
            JobNumber = pod.Job.JobNumber,
            RouteStopId = pod.RouteStopId,
            VerificationType = pod.VerificationType,
            RecipientName = pod.RecipientName,
            SignatureData = pod.SignatureData,
            Notes = pod.Notes,
            Latitude = pod.Latitude,
            Longitude = pod.Longitude,
            CompletedAtUtc = pod.CompletedAtUtc,
            VerifiedByUserId = pod.VerifiedByUserId
        };
    }

    // =========================================================================
    // HELPER METHODS & MAPPING
    // =========================================================================
    private static void RecalculateRouteEstimates(DispatchRoute route)
    {
        var ordered = route.Stops.Where(s => s.Job != null).OrderBy(s => s.SequenceOrder).ToList();
        if (ordered.Count == 0)
        {
            route.UpdateEstimates(0m, 0);
            return;
        }

        decimal totalDist = 0m;
        int totalMinutes = 0;

        for (int i = 0; i < ordered.Count; i++)
        {
            var stop = ordered[i];
            double prevLat = i == 0 ? stop.Job.Latitude : ordered[i - 1].Job.Latitude;
            double prevLon = i == 0 ? stop.Job.Longitude : ordered[i - 1].Job.Longitude;

            var dist = (decimal)CalculateHaversineDistanceKm(prevLat, prevLon, stop.Job.Latitude, stop.Job.Longitude);
            var driveMinutes = (int)Math.Ceiling((double)dist / 35.0 * 60.0); // approx 35 km/h urban speed
            var serviceMinutes = stop.Job.ServiceDurationMinutes;

            totalDist += dist;
            totalMinutes += driveMinutes + serviceMinutes;
        }

        route.UpdateEstimates(Math.Round(totalDist, 2), totalMinutes);
    }

    public static double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static DispatchJobDto MapToJobDto(DispatchJob j)
    {
        return new DispatchJobDto
        {
            Id = j.Id,
            JobNumber = j.JobNumber,
            Title = j.Title,
            JobType = j.JobType,
            Priority = j.Priority,
            Status = j.Status,
            CustomerName = j.CustomerName,
            CustomerPhone = j.CustomerPhone,
            CustomerEmail = j.CustomerEmail,
            Address = j.Address,
            Latitude = j.Latitude,
            Longitude = j.Longitude,
            TimeWindowStartUtc = j.TimeWindowStartUtc,
            TimeWindowEndUtc = j.TimeWindowEndUtc,
            ServiceDurationMinutes = j.ServiceDurationMinutes,
            WeightKg = j.WeightKg,
            VolumeM3 = j.VolumeM3,
            PackageCount = j.PackageCount,
            SpecialInstructions = j.SpecialInstructions,
            AssignedRouteId = j.AssignedRouteId,
            AssignedRouteNumber = j.AssignedRoute?.RouteNumber,
            AssignedVehicleId = j.AssignedVehicleId,
            AssignedVehicleNumber = j.AssignedVehicle?.VehicleNumber,
            AssignedDriverId = j.AssignedDriverId,
            AssignedDriverName = j.AssignedDriver != null ? $"{j.AssignedDriver.FirstName} {j.AssignedDriver.LastName}" : null,
            CompletedAtUtc = j.CompletedAtUtc,
            CancelledAtUtc = j.CancelledAtUtc,
            FailureReason = j.FailureReason,
            CreatedAtUtc = j.CreatedAtUtc
        };
    }

    private static DispatchRouteDto MapToRouteDto(DispatchRoute r)
    {
        return new DispatchRouteDto
        {
            Id = r.Id,
            RouteNumber = r.RouteNumber,
            Name = r.Name,
            ScheduledDate = r.ScheduledDate,
            Status = r.Status,
            VehicleId = r.VehicleId,
            VehicleNumber = r.Vehicle?.VehicleNumber,
            VehicleRegistration = r.Vehicle?.RegistrationNumber,
            DriverId = r.DriverId,
            DriverName = r.Driver != null ? $"{r.Driver.FirstName} {r.Driver.LastName}" : null,
            DriverPhone = r.Driver?.Phone,
            StartLocationId = r.StartLocationId,
            StartLocationName = r.StartLocation?.Name,
            EndLocationId = r.EndLocationId,
            EndLocationName = r.EndLocation?.Name,
            EstimatedDistanceKm = r.EstimatedDistanceKm,
            ActualDistanceKm = r.ActualDistanceKm,
            EstimatedDurationMinutes = r.EstimatedDurationMinutes,
            ActualDurationMinutes = r.ActualDurationMinutes,
            StartedAtUtc = r.StartedAtUtc,
            CompletedAtUtc = r.CompletedAtUtc,
            Notes = r.Notes,
            CreatedAtUtc = r.CreatedAtUtc,
            Stops = r.Stops.Select(MapToRouteStopDto).ToList()
        };
    }

    private static DispatchRouteStopDto MapToRouteStopDto(DispatchRouteStop s)
    {
        return new DispatchRouteStopDto
        {
            Id = s.Id,
            RouteId = s.RouteId,
            JobId = s.JobId,
            JobNumber = s.Job?.JobNumber ?? string.Empty,
            JobTitle = s.Job?.Title ?? string.Empty,
            JobType = s.Job?.JobType ?? DispatchJobType.Delivery,
            Priority = s.Job?.Priority ?? DispatchJobPriority.Standard,
            CustomerName = s.Job?.CustomerName ?? string.Empty,
            CustomerPhone = s.Job?.CustomerPhone ?? string.Empty,
            Address = s.Job?.Address ?? string.Empty,
            Latitude = s.Job?.Latitude ?? 0.0,
            Longitude = s.Job?.Longitude ?? 0.0,
            SequenceOrder = s.SequenceOrder,
            Status = s.Status,
            PlannedArrivalUtc = s.PlannedArrivalUtc,
            ActualArrivalUtc = s.ActualArrivalUtc,
            ActualDepartureUtc = s.ActualDepartureUtc,
            EstimatedDistanceKm = s.EstimatedDistanceKm,
            EstimatedMinutes = s.EstimatedMinutes,
            Notes = s.Notes,
            HasPod = s.ProofOfDelivery != null || s.Status == RouteStopStatus.Completed
        };
    }
}

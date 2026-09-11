using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceScheduleService : IMaintenanceScheduleService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IVehicleEngineHoursProvider _engineHoursProvider;

    public MaintenanceScheduleService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IVehicleEngineHoursProvider engineHoursProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _engineHoursProvider = engineHoursProvider;
    }

    public async Task<IReadOnlyCollection<VehicleMaintenanceDueDto>> CalculateVehicleMaintenanceAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.VehicleCategory)
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var activeAssignments = await _dbContext.VehicleMaintenancePlanAssignments
            .AsNoTracking()
            .Include(x => x.MaintenancePlan)
                .ThenInclude(p => p.Rules.Where(r => r.IsActive && !r.IsDeleted))
                    .ThenInclude(r => r.MaintenanceServiceType)
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId && x.IsActive)
            .ToListAsync(cancellationToken);

        var currentEngineHours = await _engineHoursProvider.GetCurrentEngineHoursAsync(vehicleId, cancellationToken);

        var recentCompletedRecords = await _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId && x.Status == MaintenanceRecordStatus.Completed)
            .OrderByDescending(x => x.CompletedDateTimeUtc ?? x.ServiceDateUtc)
            .ToListAsync(cancellationToken);

        var results = new List<VehicleMaintenanceDueDto>();
        var now = DateTime.UtcNow;

        foreach (var assignment in activeAssignments)
        {
            foreach (var rule in assignment.MaintenancePlan.Rules)
            {
                var lastRecord = recentCompletedRecords.FirstOrDefault(r =>
                    (r.MaintenancePlanRuleId.HasValue && r.MaintenancePlanRuleId.Value == rule.Id) ||
                    r.MaintenanceServiceTypeId == rule.MaintenanceServiceTypeId);

                var dueItem = EvaluateRule(vehicle, assignment, rule, lastRecord, currentEngineHours, now);
                results.Add(dueItem);
            }
        }

        return results.OrderBy(x => GetStatusSortOrder(x.DueStatus)).ThenBy(x => x.NextDueDateUtc).ToList();
    }

    public async Task<IReadOnlyCollection<VehicleMaintenanceDueDto>> CalculateAllVehiclesMaintenanceAsync(
        Guid? branchId = null,
        Guid? vehicleCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.VehicleCategory)
            .Where(x => x.TenantId == tenantId && x.IsActive);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        if (vehicleCategoryId.HasValue)
            query = query.Where(x => x.VehicleCategoryId == vehicleCategoryId.Value);

        var vehicles = await query.ToListAsync(cancellationToken);
        var allDueItems = new List<VehicleMaintenanceDueDto>();

        foreach (var vehicle in vehicles)
        {
            var items = await CalculateVehicleMaintenanceAsync(vehicle.Id, cancellationToken);
            allDueItems.AddRange(items);
        }

        return allDueItems;
    }

    public async Task<PagedResult<VehicleMaintenanceDueDto>> GetDueMaintenanceAsync(
        MaintenanceDueQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var allItems = await CalculateAllVehiclesMaintenanceAsync(
            query.BranchId,
            query.VehicleCategoryId,
            cancellationToken);

        var filtered = allItems.AsEnumerable();

        if (query.VehicleId.HasValue)
        {
            filtered = filtered.Where(x => x.VehicleId == query.VehicleId.Value);
        }

        if (query.ServiceTypeId.HasValue)
        {
            filtered = filtered.Where(x => x.ServiceTypeId == query.ServiceTypeId.Value);
        }

        if (query.Status.HasValue)
        {
            filtered = filtered.Where(x => x.DueStatus == query.Status.Value);
        }
        else
        {
            // Default: show Upcoming, Due, Overdue
            filtered = filtered.Where(x => x.DueStatus is MaintenanceDueStatus.Upcoming or MaintenanceDueStatus.Due or MaintenanceDueStatus.Overdue);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            filtered = filtered.Where(x =>
                x.VehicleNumber.ToLowerInvariant().Contains(search) ||
                (x.RegistrationNumber != null && x.RegistrationNumber.ToLowerInvariant().Contains(search)) ||
                x.ServiceTypeName.ToLowerInvariant().Contains(search) ||
                x.PlanName.ToLowerInvariant().Contains(search));
        }

        var totalCount = filtered.Count();
        var page = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<VehicleMaintenanceDueDto>(items, totalCount, page, pageSize);
    }

    private static VehicleMaintenanceDueDto EvaluateRule(
        Vehicle vehicle,
        VehicleMaintenancePlanAssignment assignment,
        MaintenancePlanRule rule,
        VehicleMaintenanceRecord? lastRecord,
        decimal? currentEngineHours,
        DateTime now)
    {
        var isFirstService = lastRecord == null;

        // Baseline / Last service readings
        DateTime? lastServiceDate = lastRecord?.CompletedDateTimeUtc ?? lastRecord?.ServiceDateUtc ?? assignment.EffectiveFromUtc;
        decimal? lastServiceOdometer = lastRecord?.OdometerReading ?? assignment.BaselineOdometer ?? 0m;
        decimal? lastServiceEngineHours = lastRecord?.EngineHours ?? assignment.BaselineEngineHours ?? 0m;

        decimal currentOdometer = vehicle.CurrentOdometer;
        OdometerUnit vehicleUnit = vehicle.OdometerUnit;

        // Calculations per dimension
        (decimal? nextDueOdometer, decimal? remainingDistance, MaintenanceDueStatus distanceStatus) =
            EvaluateDistance(rule, isFirstService, lastServiceOdometer.Value, currentOdometer, vehicleUnit);

        (DateTime? nextDueDateUtc, int? remainingDays, MaintenanceDueStatus calendarStatus) =
            EvaluateCalendar(rule, isFirstService, lastServiceDate.Value, now);

        (decimal? nextDueEngineHours, decimal? remainingHours, MaintenanceDueStatus engineHourStatus) =
            EvaluateEngineHours(rule, isFirstService, lastServiceEngineHours.Value, currentEngineHours);

        // Overall status resolution based on schedule type
        var overallStatus = rule.ScheduleType switch
        {
            MaintenanceScheduleType.Distance => distanceStatus,
            MaintenanceScheduleType.Calendar => calendarStatus,
            MaintenanceScheduleType.EngineHours => engineHourStatus,

            MaintenanceScheduleType.DistanceOrCalendar =>
                CombineOrStatuses(distanceStatus, calendarStatus),

            MaintenanceScheduleType.EngineHoursOrCalendar =>
                CombineOrStatuses(engineHourStatus, calendarStatus),

            MaintenanceScheduleType.DistanceAndCalendar =>
                CombineAndStatuses(distanceStatus, calendarStatus),

            _ => CombineOrStatuses(distanceStatus, calendarStatus)
        };

        return new VehicleMaintenanceDueDto(
            vehicle.Id,
            vehicle.VehicleNumber,
            vehicle.RegistrationNumber,
            vehicle.DisplayName,
            vehicle.BranchId,
            vehicle.Branch?.Name,
            vehicle.VehicleCategoryId,
            vehicle.VehicleCategory?.Name ?? string.Empty,
            assignment.MaintenancePlanId,
            assignment.MaintenancePlan.Code,
            assignment.MaintenancePlan.Name,
            rule.Id,
            rule.MaintenanceServiceTypeId,
            rule.MaintenanceServiceType.Code,
            rule.MaintenanceServiceType.Name,
            rule.MaintenanceServiceType.Category,
            rule.MaintenanceServiceType.Category.ToString(),
            rule.ScheduleType,
            rule.ScheduleType.ToString(),
            lastRecord != null ? lastServiceDate : null,
            lastRecord?.OdometerReading,
            lastRecord?.EngineHours,
            currentOdometer,
            currentEngineHours,
            vehicleUnit,
            nextDueDateUtc,
            nextDueOdometer,
            nextDueEngineHours,
            remainingDistance,
            remainingHours,
            remainingDays,
            overallStatus,
            overallStatus.ToString(),
            rule.IsMandatory,
            now);
    }

    private static (decimal? NextDue, decimal? Remaining, MaintenanceDueStatus Status) EvaluateDistance(
        MaintenancePlanRule rule,
        bool isFirstService,
        decimal lastOdometer,
        decimal currentOdometer,
        OdometerUnit vehicleUnit)
    {
        // Get interval in vehicle's unit
        decimal? intervalInVehicleUnit = null;
        if (vehicleUnit == OdometerUnit.Miles)
        {
            if (rule.IntervalMiles.HasValue) intervalInVehicleUnit = rule.IntervalMiles.Value;
            else if (rule.IntervalKilometers.HasValue) intervalInVehicleUnit = MaintenanceUnitConverter.FromKilometers(rule.IntervalKilometers.Value, OdometerUnit.Miles);
        }
        else
        {
            if (rule.IntervalKilometers.HasValue) intervalInVehicleUnit = rule.IntervalKilometers.Value;
            else if (rule.IntervalMiles.HasValue) intervalInVehicleUnit = MaintenanceUnitConverter.ToKilometers(rule.IntervalMiles.Value, OdometerUnit.Miles);
        }

        if (!intervalInVehicleUnit.HasValue || intervalInVehicleUnit.Value <= 0)
        {
            return (null, null, MaintenanceDueStatus.NotApplicable);
        }

        decimal nextDue;
        if (isFirstService && rule.InitialDueKilometers.HasValue && vehicleUnit == OdometerUnit.Kilometers)
        {
            nextDue = rule.InitialDueKilometers.Value;
        }
        else if (isFirstService && rule.InitialDueKilometers.HasValue && vehicleUnit == OdometerUnit.Miles)
        {
            nextDue = MaintenanceUnitConverter.FromKilometers(rule.InitialDueKilometers.Value, OdometerUnit.Miles);
        }
        else
        {
            nextDue = lastOdometer + intervalInVehicleUnit.Value;
        }

        decimal reminder = 0m;
        if (rule.ReminderBeforeKilometers.HasValue)
        {
            reminder = vehicleUnit == OdometerUnit.Miles
                ? MaintenanceUnitConverter.FromKilometers(rule.ReminderBeforeKilometers.Value, OdometerUnit.Miles)
                : rule.ReminderBeforeKilometers.Value;
        }

        decimal tolerance = 0m;
        if (rule.ToleranceKilometers.HasValue)
        {
            tolerance = vehicleUnit == OdometerUnit.Miles
                ? MaintenanceUnitConverter.FromKilometers(rule.ToleranceKilometers.Value, OdometerUnit.Miles)
                : rule.ToleranceKilometers.Value;
        }

        decimal remaining = nextDue - currentOdometer;

        MaintenanceDueStatus status;
        if (currentOdometer > nextDue + tolerance)
        {
            status = MaintenanceDueStatus.Overdue;
        }
        else if (currentOdometer >= nextDue)
        {
            status = MaintenanceDueStatus.Due;
        }
        else if (currentOdometer >= nextDue - reminder)
        {
            status = MaintenanceDueStatus.Upcoming;
        }
        else
        {
            status = MaintenanceDueStatus.NotDue;
        }

        return (nextDue, remaining, status);
    }

    private static (DateTime? NextDueDateUtc, int? RemainingDays, MaintenanceDueStatus Status) EvaluateCalendar(
        MaintenancePlanRule rule,
        bool isFirstService,
        DateTime lastServiceDate,
        DateTime now)
    {
        if (!rule.IntervalDays.HasValue && !rule.IntervalMonths.HasValue)
        {
            return (null, null, MaintenanceDueStatus.NotApplicable);
        }

        DateTime nextDueDate;
        if (isFirstService && rule.InitialDueDateUtc.HasValue)
        {
            nextDueDate = rule.InitialDueDateUtc.Value;
        }
        else if (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0)
        {
            nextDueDate = lastServiceDate.AddMonths(rule.IntervalMonths.Value);
        }
        else if (rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0)
        {
            nextDueDate = lastServiceDate.AddDays(rule.IntervalDays.Value);
        }
        else
        {
            return (null, null, MaintenanceDueStatus.NotApplicable);
        }

        int reminderDays = rule.ReminderBeforeDays ?? 0;
        int toleranceDays = rule.ToleranceDays ?? 0;

        var nowDate = now.Date;
        var targetDate = nextDueDate.Date;
        int remainingDays = (int)(targetDate - nowDate).TotalDays;

        MaintenanceDueStatus status;
        if (nowDate > targetDate.AddDays(toleranceDays))
        {
            status = MaintenanceDueStatus.Overdue;
        }
        else if (nowDate >= targetDate)
        {
            status = MaintenanceDueStatus.Due;
        }
        else if (nowDate >= targetDate.AddDays(-reminderDays))
        {
            status = MaintenanceDueStatus.Upcoming;
        }
        else
        {
            status = MaintenanceDueStatus.NotDue;
        }

        return (nextDueDate, remainingDays, status);
    }

    private static (decimal? NextDue, decimal? Remaining, MaintenanceDueStatus Status) EvaluateEngineHours(
        MaintenancePlanRule rule,
        bool isFirstService,
        decimal lastEngineHours,
        decimal? currentEngineHours)
    {
        if (!rule.IntervalEngineHours.HasValue || rule.IntervalEngineHours.Value <= 0)
        {
            return (null, null, MaintenanceDueStatus.NotApplicable);
        }

        decimal nextDue = (isFirstService && rule.InitialDueEngineHours.HasValue)
            ? rule.InitialDueEngineHours.Value
            : lastEngineHours + rule.IntervalEngineHours.Value;

        if (!currentEngineHours.HasValue)
        {
            return (nextDue, null, MaintenanceDueStatus.NotDue);
        }

        decimal reminder = rule.ReminderBeforeEngineHours ?? 0m;
        decimal tolerance = rule.ToleranceHours ?? 0m;
        decimal remaining = nextDue - currentEngineHours.Value;

        MaintenanceDueStatus status;
        if (currentEngineHours.Value > nextDue + tolerance)
        {
            status = MaintenanceDueStatus.Overdue;
        }
        else if (currentEngineHours.Value >= nextDue)
        {
            status = MaintenanceDueStatus.Due;
        }
        else if (currentEngineHours.Value >= nextDue - reminder)
        {
            status = MaintenanceDueStatus.Upcoming;
        }
        else
        {
            status = MaintenanceDueStatus.NotDue;
        }

        return (nextDue, remaining, status);
    }

    private static MaintenanceDueStatus CombineOrStatuses(MaintenanceDueStatus a, MaintenanceDueStatus b)
    {
        if (a == MaintenanceDueStatus.NotApplicable) return b;
        if (b == MaintenanceDueStatus.NotApplicable) return a;

        // Overdue > Due > Upcoming > NotDue
        if (a == MaintenanceDueStatus.Overdue || b == MaintenanceDueStatus.Overdue) return MaintenanceDueStatus.Overdue;
        if (a == MaintenanceDueStatus.Due || b == MaintenanceDueStatus.Due) return MaintenanceDueStatus.Due;
        if (a == MaintenanceDueStatus.Upcoming || b == MaintenanceDueStatus.Upcoming) return MaintenanceDueStatus.Upcoming;
        return MaintenanceDueStatus.NotDue;
    }

    private static MaintenanceDueStatus CombineAndStatuses(MaintenanceDueStatus a, MaintenanceDueStatus b)
    {
        if (a == MaintenanceDueStatus.NotApplicable) return b;
        if (b == MaintenanceDueStatus.NotApplicable) return a;

        // Both must reach threshold
        var aRank = GetStatusSeverityRank(a);
        var bRank = GetStatusSeverityRank(b);

        var minRank = Math.Min(aRank, bRank);
        return minRank switch
        {
            3 => MaintenanceDueStatus.Overdue,
            2 => MaintenanceDueStatus.Due,
            1 => MaintenanceDueStatus.Upcoming,
            _ => MaintenanceDueStatus.NotDue
        };
    }

    private static int GetStatusSeverityRank(MaintenanceDueStatus status) => status switch
    {
        MaintenanceDueStatus.Overdue => 3,
        MaintenanceDueStatus.Due => 2,
        MaintenanceDueStatus.Upcoming => 1,
        _ => 0
    };

    private static int GetStatusSortOrder(MaintenanceDueStatus status) => status switch
    {
        MaintenanceDueStatus.Overdue => 1,
        MaintenanceDueStatus.Due => 2,
        MaintenanceDueStatus.Upcoming => 3,
        MaintenanceDueStatus.NotDue => 4,
        MaintenanceDueStatus.Completed => 5,
        _ => 6
    };

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}

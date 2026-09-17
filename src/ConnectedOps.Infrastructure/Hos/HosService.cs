using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Hos;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Hos;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Hos;

public sealed class HosService : IHosService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public HosService(
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

    public async Task<HosRuleConfigurationDto> GetPolicyAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var policy = await GetOrCreateDefaultPolicyAsync(tenantId, cancellationToken);
        return MapToPolicyDto(policy);
    }

    public async Task<HosRuleConfigurationDto> UpdatePolicyAsync(UpdateHosPolicyRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var policy = await GetOrCreateDefaultPolicyAsync(tenantId, cancellationToken);

        if (request.PresetType != HosPresetType.Custom)
        {
            policy.ApplyPreset(request.PresetType, _currentUserContext.UserId);
        }
        else
        {
            policy.UpdateCustomPolicy(
                maxDrivingHours: request.MaxDrivingHoursPerShift ?? policy.MaxDrivingHoursPerShift,
                maxShiftHours: request.MaxShiftDutyHours ?? policy.MaxShiftDutyHours,
                driveHoursBeforeBreak: request.DriveHoursBeforeMandatoryBreak ?? policy.DriveHoursBeforeMandatoryBreak,
                breakMinutes: request.MandatoryBreakMinutes ?? policy.MandatoryBreakMinutes,
                minOffDutyHours: request.MinConsecutiveOffDutyHours ?? policy.MinConsecutiveOffDutyHours,
                cycleDays: request.CycleDays ?? policy.CycleDays,
                cycleMaxHours: request.CycleMaxDutyHours ?? policy.CycleMaxDutyHours,
                updatedBy: _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToPolicyDto(policy);
    }

    public async Task<HosDriverClocksDto> GetDriverClocksAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var policy = await GetOrCreateDefaultPolicyAsync(tenantId, cancellationToken);

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID '{driverId}' was not found.");

        var logs = await _dbContext.HosLogEntries
            .Where(l => l.TenantId == tenantId && l.DriverId == driverId)
            .OrderByDescending(l => l.StartedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var currentLog = logs.FirstOrDefault();
        var currentStatus = currentLog?.Status ?? DutyStatus.OffDuty;
        var currentStatusStart = currentLog?.StartedAtUtc ?? DateTime.UtcNow;

        // Find shift start: working backwards, find last rest period >= MinConsecutiveOffDutyHours
        var shiftLogs = new List<HosLogEntry>();
        foreach (var log in logs)
        {
            if ((log.Status == DutyStatus.OffDuty || log.Status == DutyStatus.SleeperBerth) &&
                log.DurationHours >= policy.MinConsecutiveOffDutyHours)
            {
                break;
            }
            shiftLogs.Add(log);
        }

        // Calculate shift hours
        var shiftDrivingHours = shiftLogs
            .Where(l => l.Status == DutyStatus.Driving)
            .Sum(l => l.DurationHours);

        var shiftDutyHours = shiftLogs
            .Where(l => l.Status == DutyStatus.Driving || l.Status == DutyStatus.OnDutyNotDriving || l.Status == DutyStatus.YardMove)
            .Sum(l => l.DurationHours);

        // Continuous driving without break >= MandatoryBreakMinutes
        var continuousDriveHours = 0.0;
        foreach (var log in shiftLogs)
        {
            if ((log.Status == DutyStatus.OffDuty || log.Status == DutyStatus.SleeperBerth || log.Status == DutyStatus.OnDutyNotDriving) &&
                log.DurationHours >= (policy.MandatoryBreakMinutes / 60.0))
            {
                break;
            }
            if (log.Status == DutyStatus.Driving)
            {
                continuousDriveHours += log.DurationHours;
            }
        }

        // Cycle hours (last CycleDays days)
        var cycleCutoff = DateTime.UtcNow.AddDays(-policy.CycleDays);
        var cycleDutyHours = await _dbContext.HosLogEntries
            .Where(l => l.TenantId == tenantId && l.DriverId == driverId && l.StartedAtUtc >= cycleCutoff &&
                        (l.Status == DutyStatus.Driving || l.Status == DutyStatus.OnDutyNotDriving || l.Status == DutyStatus.YardMove))
            .SumAsync(l => (double?)((l.EndedAtUtc ?? DateTime.UtcNow) - l.StartedAtUtc).TotalHours, cancellationToken) ?? 0.0;

        var remainingDrive = Math.Max(0.0, Math.Round(policy.MaxDrivingHoursPerShift - shiftDrivingHours, 1));
        var remainingShift = Math.Max(0.0, Math.Round(policy.MaxShiftDutyHours - shiftDutyHours, 1));
        var timeUntilBreak = Math.Max(0.0, Math.Round(policy.DriveHoursBeforeMandatoryBreak - continuousDriveHours, 1));
        var remainingCycle = Math.Max(0.0, Math.Round(policy.CycleMaxDutyHours - cycleDutyHours, 1));

        // Auto-detect violations if thresholds breached
        var violations = await _dbContext.HosViolations
            .Where(v => v.TenantId == tenantId && v.DriverId == driverId && !v.IsAcknowledged)
            .OrderByDescending(v => v.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        if (shiftDrivingHours > policy.MaxDrivingHoursPerShift &&
            !violations.Any(v => v.ViolationType == HosViolationType.DrivingLimitExceeded && v.OccurredAtUtc >= DateTime.UtcNow.AddHours(-4)))
        {
            var viol = new HosViolation(tenantId, driverId, HosViolationType.DrivingLimitExceeded, DateTime.UtcNow, (int)((shiftDrivingHours - policy.MaxDrivingHoursPerShift) * 60), $"Exceeded configured maximum driving limit of {policy.MaxDrivingHoursPerShift} hours.");
            _dbContext.HosViolations.Add(viol);
            violations.Add(viol);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (shiftDutyHours > policy.MaxShiftDutyHours &&
            !violations.Any(v => v.ViolationType == HosViolationType.ShiftWindowExceeded && v.OccurredAtUtc >= DateTime.UtcNow.AddHours(-4)))
        {
            var viol = new HosViolation(tenantId, driverId, HosViolationType.ShiftWindowExceeded, DateTime.UtcNow, (int)((shiftDutyHours - policy.MaxShiftDutyHours) * 60), $"Exceeded configured maximum shift duty window of {policy.MaxShiftDutyHours} hours.");
            _dbContext.HosViolations.Add(viol);
            violations.Add(viol);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new HosDriverClocksDto(
            DriverId: driverId,
            DriverName: $"{driver.FirstName} {driver.LastName}".Trim(),
            CurrentStatus: currentStatus,
            CurrentStatusName: currentStatus.ToString(),
            CurrentStatusStartedAtUtc: currentStatusStart,
            RemainingDrivingHours: remainingDrive,
            RemainingShiftDutyHours: remainingShift,
            HoursUntilBreakRequired: timeUntilBreak,
            CycleHoursUsed: Math.Round(cycleDutyHours, 1),
            CycleHoursRemaining: remainingCycle,
            ActivePolicy: MapToPolicyDto(policy),
            ActiveViolations: violations.Select(MapToViolationDto).ToList());
    }

    public async Task<HosLogEntryDto> ChangeDutyStatusAsync(Guid driverId, ChangeDutyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID '{driverId}' was not found.");

        // Find active open log entry and close it
        var activeLog = await _dbContext.HosLogEntries
            .Where(l => l.TenantId == tenantId && l.DriverId == driverId && l.EndedAtUtc == null)
            .OrderByDescending(l => l.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        if (activeLog != null)
        {
            activeLog.EndLog(now, request.Odometer, _currentUserContext.UserId);
        }

        var newLog = new HosLogEntry(
            tenantId,
            driverId,
            request.Status,
            now,
            vehicleId: request.VehicleId,
            startOdometer: request.Odometer,
            locationName: request.LocationName,
            latitude: request.Latitude,
            longitude: request.Longitude,
            notes: request.Notes,
            createdByUserId: _currentUserContext.UserId);

        if (request.VehicleId.HasValue)
        {
            var v = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Id == request.VehicleId.Value && x.TenantId == tenantId, cancellationToken);
            newLog.Vehicle = v;
        }
        newLog.Driver = driver;

        await _dbContext.HosLogEntries.AddAsync(newLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToLogDto(newLog);
    }

    public async Task<PagedResult<HosLogEntryDto>> GetLogsPagedAsync(HosLogFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.HosLogEntries
            .Include(l => l.Driver)
            .Include(l => l.Vehicle)
            .Where(l => l.TenantId == tenantId);

        if (request.DriverId.HasValue)
            query = query.Where(l => l.DriverId == request.DriverId.Value);

        if (request.Date.HasValue)
        {
            var dateStart = request.Date.Value.Date;
            var dateEnd = dateStart.AddDays(1);
            query = query.Where(l => l.StartedAtUtc >= dateStart && l.StartedAtUtc < dateEnd);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.StartedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HosLogEntryDto>(
            items.Select(MapToLogDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<PagedResult<HosViolationDto>> GetViolationsPagedAsync(HosViolationFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.HosViolations
            .Include(v => v.Driver)
            .Where(v => v.TenantId == tenantId);

        if (request.DriverId.HasValue)
            query = query.Where(v => v.DriverId == request.DriverId.Value);

        if (request.ViolationType.HasValue)
            query = query.Where(v => v.ViolationType == request.ViolationType.Value);

        if (request.IsAcknowledged.HasValue)
            query = query.Where(v => v.IsAcknowledged == request.IsAcknowledged.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.OccurredAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HosViolationDto>(
            items.Select(MapToViolationDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<HosViolationDto> AcknowledgeViolationAsync(Guid violationId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var violation = await _dbContext.HosViolations
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == violationId && v.TenantId == tenantId, cancellationToken);

        if (violation == null)
            throw new KeyNotFoundException($"HOS Violation with ID '{violationId}' was not found.");

        violation.Acknowledge(_currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToViolationDto(violation);
    }

    public async Task<HosRoadsideReportDto> GenerateRoadsideReportAsync(Guid driverId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var policy = await GetOrCreateDefaultPolicyAsync(tenantId, cancellationToken);

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver == null)
            throw new KeyNotFoundException($"Driver with ID '{driverId}' was not found.");

        var dayStart = dateUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var logs = await _dbContext.HosLogEntries
            .Include(l => l.Driver)
            .Include(l => l.Vehicle)
            .Where(l => l.TenantId == tenantId && l.DriverId == driverId && l.StartedAtUtc >= dayStart && l.StartedAtUtc < dayEnd)
            .OrderBy(l => l.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var violations = await _dbContext.HosViolations
            .Include(v => v.Driver)
            .Where(v => v.TenantId == tenantId && v.DriverId == driverId && v.OccurredAtUtc >= dayStart && v.OccurredAtUtc < dayEnd)
            .OrderBy(v => v.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var totalDrive = logs.Where(l => l.Status == DutyStatus.Driving).Sum(l => l.DurationHours);
        var totalOnDuty = logs.Where(l => l.Status == DutyStatus.OnDutyNotDriving || l.Status == DutyStatus.YardMove).Sum(l => l.DurationHours);
        var totalOffDuty = logs.Where(l => l.Status == DutyStatus.OffDuty || l.Status == DutyStatus.PersonalConveyance).Sum(l => l.DurationHours);
        var totalSleeper = logs.Where(l => l.Status == DutyStatus.SleeperBerth).Sum(l => l.DurationHours);

        var currentPlate = logs.LastOrDefault(l => l.Vehicle != null)?.Vehicle?.RegistrationNumber ?? "N/A";

        return new HosRoadsideReportDto(
            DriverId: driverId,
            DriverName: $"{driver.FirstName} {driver.LastName}".Trim(),
            DriverLicenseNumber: driver.PrimaryLicenseNumber ?? "N/A",
            DateUtc: dateUtc.Date,
            CurrentVehiclePlate: currentPlate,
            AppliedPolicy: MapToPolicyDto(policy),
            DayLogs: logs.Select(MapToLogDto).ToList(),
            DayViolations: violations.Select(MapToViolationDto).ToList(),
            TotalDrivingHours: Math.Round(totalDrive, 1),
            TotalOnDutyHours: Math.Round(totalOnDuty, 1),
            TotalOffDutyHours: Math.Round(totalOffDuty, 1),
            TotalSleeperHours: Math.Round(totalSleeper, 1));
    }

    private async Task<HosRuleConfiguration> GetOrCreateDefaultPolicyAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var policy = await _dbContext.HosRuleConfigurations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (policy == null)
        {
            policy = new HosRuleConfiguration(tenantId, HosPresetType.GccUaeStandard);
            await _dbContext.HosRuleConfigurations.AddAsync(policy, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return policy;
    }

    private static HosRuleConfigurationDto MapToPolicyDto(HosRuleConfiguration p) =>
        new(
            Id: p.Id,
            PresetType: p.PresetType,
            PresetName: p.PresetType switch
            {
                HosPresetType.GccUaeStandard => "GCC / UAE Standard (10h Drive / 12h Shift / 60h Cycle)",
                HosPresetType.EuTachograph => "EU Tachograph EC 561/2006 (9h Drive / 4.5h Break / 56h Cycle)",
                HosPresetType.UsFmcsa => "US FMCSA Property (11h Drive / 14h Window / 70h Cycle)",
                _ => "Custom Corporate Fleet Policy"
            },
            MaxDrivingHoursPerShift: p.MaxDrivingHoursPerShift,
            MaxShiftDutyHours: p.MaxShiftDutyHours,
            DriveHoursBeforeMandatoryBreak: p.DriveHoursBeforeMandatoryBreak,
            MandatoryBreakMinutes: p.MandatoryBreakMinutes,
            MinConsecutiveOffDutyHours: p.MinConsecutiveOffDutyHours,
            CycleDays: p.CycleDays,
            CycleMaxDutyHours: p.CycleMaxDutyHours);

    private static HosLogEntryDto MapToLogDto(HosLogEntry l) =>
        new(
            Id: l.Id,
            DriverId: l.DriverId,
            DriverName: l.Driver != null ? $"{l.Driver.FirstName} {l.Driver.LastName}".Trim() : "N/A",
            Status: l.Status,
            StatusName: l.Status.ToString(),
            StartedAtUtc: l.StartedAtUtc,
            EndedAtUtc: l.EndedAtUtc,
            DurationHours: Math.Round(l.DurationHours, 2),
            VehicleId: l.VehicleId,
            VehiclePlateNumber: l.Vehicle != null ? (l.Vehicle.RegistrationNumber ?? l.Vehicle.VehicleNumber) : null,
            StartOdometer: l.StartOdometer,
            EndOdometer: l.EndOdometer,
            LocationName: l.LocationName,
            Latitude: l.Latitude,
            Longitude: l.Longitude,
            Notes: l.Notes,
            IsCertified: l.IsCertified,
            CertifiedAtUtc: l.CertifiedAtUtc);

    private static HosViolationDto MapToViolationDto(HosViolation v) =>
        new(
            Id: v.Id,
            DriverId: v.DriverId,
            DriverName: v.Driver != null ? $"{v.Driver.FirstName} {v.Driver.LastName}".Trim() : "N/A",
            ViolationType: v.ViolationType,
            ViolationTypeName: v.ViolationType switch
            {
                HosViolationType.DrivingLimitExceeded => "Driving Limit Exceeded",
                HosViolationType.ShiftWindowExceeded => "Shift Duty Window Exceeded",
                HosViolationType.MissingRestBreak => "Mandatory Rest Break Omitted",
                HosViolationType.CycleLimitExceeded => "Cycle Duty Hours Exceeded",
                _ => v.ViolationType.ToString()
            },
            OccurredAtUtc: v.OccurredAtUtc,
            DurationMinutes: v.DurationMinutes,
            Notes: v.Notes,
            IsAcknowledged: v.IsAcknowledged,
            AcknowledgedAtUtc: v.AcknowledgedAtUtc);
}

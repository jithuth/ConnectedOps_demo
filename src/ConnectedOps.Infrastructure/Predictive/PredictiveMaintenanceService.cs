using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Predictive;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Predictive;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Predictive;

public sealed class PredictiveMaintenanceService : IPredictiveMaintenanceService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<PredictiveMaintenanceService> _logger;

    public PredictiveMaintenanceService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<PredictiveMaintenanceService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for predictive maintenance operations.");
    }

    public async Task<PredictiveFleetDashboardDto> GetFleetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.Status == VehicleStatus.Active)
            .ToListAsync(cancellationToken);

        var totalMonitored = vehicles.Count;

        var allHealths = await _dbContext.VehicleSubsystemHealths
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var alerts = await _dbContext.PredictiveMaintenanceAlerts
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId && a.Status == PredictiveRecommendationStatus.Active)
            .OrderByDescending(a => a.RiskLevel)
            .ThenByDescending(a => a.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        // Group health scores by vehicle
        var vehicleTwins = new List<VehicleDigitalTwinHealthDto>();
        decimal totalHealthSum = 0m;
        int healthEvaluatedCount = 0;
        int criticalCount = 0;
        int watchCount = 0;

        foreach (var v in vehicles)
        {
            var vHealths = allHealths.Where(h => h.VehicleId == v.Id).ToList();
            var dtos = vHealths.Select(MapSubsystemToDto).ToList();

            decimal overallScore = dtos.Count > 0 ? Math.Round(dtos.Average(s => s.HealthScore), 1) : 88.0m;
            totalHealthSum += overallScore;
            healthEvaluatedCount++;

            var vAlerts = alerts.Where(a => a.VehicleId == v.Id).ToList();
            var overallRisk = ComputeRiskLevel(overallScore, dtos);

            if (overallRisk == PredictiveRiskLevel.CriticalFailureImminent)
                criticalCount++;
            else if (overallRisk == PredictiveRiskLevel.ElevatedWarning || overallRisk == PredictiveRiskLevel.ModerateWatch)
                watchCount++;

            vehicleTwins.Add(new VehicleDigitalTwinHealthDto(
                v.Id,
                v.VehicleNumber,
                v.DisplayName,
                v.RegistrationNumber,
                overallScore,
                overallRisk,
                overallRisk.ToString(),
                vAlerts.Count,
                dtos,
                dtos.Count > 0 ? dtos.Max(s => s.LastEvaluatedAtUtc) : DateTime.UtcNow));
        }

        decimal avgFleetHealth = healthEvaluatedCount > 0
            ? Math.Round(totalHealthSum / healthEvaluatedCount, 1)
            : 100m;

        decimal potentialSavings = alerts.Sum(a => a.EstimatedRepairCost * 1.75m);

        var urgentAlertDtos = alerts.Take(10).Select(MapAlertToDto).ToList();
        var topAtRisk = vehicleTwins
            .OrderBy(v => v.OverallHealthScore)
            .Take(8)
            .ToList();

        return new PredictiveFleetDashboardDto(
            totalMonitored,
            avgFleetHealth,
            criticalCount,
            watchCount,
            alerts.Count,
            potentialSavings,
            urgentAlertDtos,
            topAtRisk);
    }

    public async Task<VehicleDigitalTwinHealthDto?> GetVehicleDigitalTwinAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Id == vehicleId, cancellationToken);

        if (vehicle == null) return null;

        var healths = await _dbContext.VehicleSubsystemHealths
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);

        // If none exist yet, perform immediate diagnostic scan for this vehicle
        if (healths.Count == 0)
        {
            await RunFleetDiagnosticEvaluationAsync(vehicleId, cancellationToken);
            healths = await _dbContext.VehicleSubsystemHealths
                .AsNoTracking()
                .Where(h => h.TenantId == tenantId && h.VehicleId == vehicleId)
                .ToListAsync(cancellationToken);
        }

        var dtos = healths.Select(MapSubsystemToDto).ToList();
        decimal overallScore = dtos.Count > 0 ? Math.Round(dtos.Average(s => s.HealthScore), 1) : 85m;
        var overallRisk = ComputeRiskLevel(overallScore, dtos);

        var activeAlerts = await _dbContext.PredictiveMaintenanceAlerts
            .CountAsync(a => a.TenantId == tenantId && a.VehicleId == vehicleId && a.Status == PredictiveRecommendationStatus.Active, cancellationToken);

        return new VehicleDigitalTwinHealthDto(
            vehicle.Id,
            vehicle.VehicleNumber,
            vehicle.DisplayName,
            vehicle.RegistrationNumber,
            overallScore,
            overallRisk,
            overallRisk.ToString(),
            activeAlerts,
            dtos,
            dtos.Count > 0 ? dtos.Max(s => s.LastEvaluatedAtUtc) : DateTime.UtcNow);
    }

    public async Task<PagedResult<PredictiveAlertDto>> GetPredictiveAlertsPagedAsync(
        Guid? vehicleId = null,
        PredictiveRiskLevel? riskLevel = null,
        PredictiveRecommendationStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.PredictiveMaintenanceAlerts
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId);

        if (vehicleId.HasValue)
        {
            query = query.Where(a => a.VehicleId == vehicleId.Value);
        }

        if (riskLevel.HasValue)
        {
            query = query.Where(a => a.RiskLevel == riskLevel.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.RiskLevel)
            .ThenByDescending(a => a.DetectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapAlertToDto(a))
            .ToListAsync(cancellationToken);

        return new PagedResult<PredictiveAlertDto>(items, total, page, pageSize);
    }

    public async Task<int> RunFleetDiagnosticEvaluationAsync(Guid? vehicleId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        _logger.LogInformation("Running fleet AI predictive diagnostic evaluation on Tenant: {TenantId}", tenantId);

        var vehiclesQuery = _dbContext.Vehicles.Where(v => v.TenantId == tenantId && v.Status == VehicleStatus.Active);
        if (vehicleId.HasValue)
        {
            vehiclesQuery = vehiclesQuery.Where(v => v.Id == vehicleId.Value);
        }

        var vehicles = await vehiclesQuery.ToListAsync(cancellationToken);
        if (vehicles.Count == 0) return 0;

        int newAlertsCount = 0;
        var existingHealths = await _dbContext.VehicleSubsystemHealths
            .Where(h => h.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var existingAlerts = await _dbContext.PredictiveMaintenanceAlerts
            .Where(a => a.TenantId == tenantId && a.Status == PredictiveRecommendationStatus.Active)
            .ToListAsync(cancellationToken);

        var categories = Enum.GetValues<SubsystemCategory>();

        foreach (var v in vehicles)
        {
            // Calculate deterministic pseudo-telemetry metrics based on vehicle properties
            var odo = v.CurrentOdometer;
            var seed = (int)(v.Id.GetHashCode() ^ (long)odo);
            var random = new Random(seed);

            foreach (var cat in categories)
            {
                var healthEntity = existingHealths.FirstOrDefault(h => h.VehicleId == v.Id && h.Subsystem == cat);

                // Derive health based on category and odometer
                decimal divisor = cat switch
                {
                    SubsystemCategory.TiresSuspension => 2000m,
                    SubsystemCategory.BrakingChassis => 2500m,
                    SubsystemCategory.ElectricalBattery => 3000m,
                    SubsystemCategory.PowertrainEngine => 3500m,
                    SubsystemCategory.TransmissionDrivetrain => 4000m,
                    _ => 3000m
                };
                decimal baseHealth = 96m - (odo / divisor);
                baseHealth += (decimal)(random.NextDouble() * 8 - 4);
                baseHealth = Math.Clamp(baseHealth, 25m, 100m);

                var trend = baseHealth < 60 ? HealthTrend.Degrading : (baseHealth > 85 ? HealthTrend.Improving : HealthTrend.Stable);
                int anomalyCount = baseHealth < 70 ? (int)((100 - baseHealth) / 8) : 0;
                int? rulDays = baseHealth < 75 ? Math.Max(3, (int)(baseHealth * 0.9m)) : null;

                string? notes = GenerateDiagnosticNote(cat, baseHealth);

                if (healthEntity == null)
                {
                    healthEntity = new VehicleSubsystemHealth(
                        tenantId,
                        v.Id,
                        cat,
                        baseHealth,
                        trend,
                        anomalyCount,
                        rulDays,
                        notes);
                    _dbContext.VehicleSubsystemHealths.Add(healthEntity);
                }
                else
                {
                    healthEntity.UpdateHealth(baseHealth, trend, anomalyCount, rulDays, notes);
                }

                // If health is below threshold, create or refresh an alert
                if (baseHealth < 65m)
                {
                    var existingAlert = existingAlerts.FirstOrDefault(a => a.VehicleId == v.Id && a.Subsystem == cat);
                    if (existingAlert == null)
                    {
                        var risk = baseHealth < 45m ? PredictiveRiskLevel.CriticalFailureImminent : PredictiveRiskLevel.ElevatedWarning;
                        decimal prob = Math.Round(100m - (baseHealth * 0.75m), 1);
                        int rul = rulDays ?? 14;

                        var (title, symptom, action, cost) = GetRecommendation(cat, baseHealth);

                        var alert = new PredictiveMaintenanceAlert(
                            tenantId,
                            v.Id,
                            cat,
                            risk,
                            prob,
                            rul,
                            title,
                            symptom,
                            action,
                            cost,
                            "USD");

                        _dbContext.PredictiveMaintenanceAlerts.Add(alert);
                        newAlertsCount++;
                    }
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Diagnostic scan complete. Generated/Refreshed {AlertsCount} alerts for {VehiclesCount} vehicles", newAlertsCount, vehicles.Count);
        return newAlertsCount;
    }

    public async Task<Guid> PromoteAlertToWorkOrderAsync(Guid alertId, PromoteToWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var alert = await _dbContext.PredictiveMaintenanceAlerts
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alertId, cancellationToken);

        if (alert == null)
            throw new InvalidOperationException($"Predictive alert '{alertId}' not found.");

        if (alert.Status == PredictiveRecommendationStatus.WorkOrderCreated)
            return alert.PromotedMaintenanceRecordId ?? Guid.Empty;

        // Ensure maintenance service type exists
        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Code == "PRED-MAINT", cancellationToken);

        if (serviceType == null)
        {
            serviceType = new MaintenanceServiceType(
                tenantId,
                "PRED-MAINT",
                "AI Predictive Component Service",
                MaintenanceServiceCategory.Corrective,
                "Automated work order initiated by AI predictive failure detection");
            _dbContext.MaintenanceServiceTypes.Add(serviceType);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var scheduledDate = request.ScheduledDateUtc ?? DateTime.UtcNow.AddDays(Math.Min(alert.EstimatedRulDays, 5));
        var refNum = $"PRED-{DateTime.UtcNow:yyyyMMdd}-{alert.Id.ToString("N")[..6].ToUpperInvariant()}";
        var description = $"[AI PREDICTIVE] {alert.ComponentTitle} - {alert.RecommendedAction}. {request.Notes}".Trim();

        var record = new VehicleMaintenanceRecord(
            tenantId,
            alert.VehicleId,
            serviceType.Id,
            scheduledDate,
            maintenanceType: VehicleMaintenanceType.Preventive,
            status: MaintenanceRecordStatus.Scheduled,
            referenceNumber: refNum,
            description: description,
            technicianNotes: $"Triggered from alert with {alert.FailureProbability}% failure probability. RUL: {alert.EstimatedRulDays} days.",
            currencyCode: alert.Currency,
            createdByUserId: _currentUserContext.UserId);

        _dbContext.VehicleMaintenanceRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        alert.PromoteToWorkOrder(record.Id, _currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Promoted alert {AlertId} to Work Order {RecordId} for Vehicle {VehicleId}",
            alert.Id, record.Id, alert.VehicleId);

        return record.Id;
    }

    public async Task<bool> DismissAlertAsync(Guid alertId, string reason, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var alert = await _dbContext.PredictiveMaintenanceAlerts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alertId, cancellationToken);

        if (alert == null) return false;

        alert.Dismiss(reason, _currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PredictiveRiskLevel ComputeRiskLevel(decimal overallScore, List<SubsystemHealthScoreDto> subsystems)
    {
        if (overallScore < 50m || subsystems.Any(s => s.HealthScore < 40m))
            return PredictiveRiskLevel.CriticalFailureImminent;

        if (overallScore < 70m || subsystems.Any(s => s.HealthScore < 60m))
            return PredictiveRiskLevel.ElevatedWarning;

        if (overallScore < 85m || subsystems.Any(s => s.HealthScore < 75m))
            return PredictiveRiskLevel.ModerateWatch;

        return PredictiveRiskLevel.LowRisk;
    }

    private static SubsystemHealthScoreDto MapSubsystemToDto(VehicleSubsystemHealth h)
    {
        return new SubsystemHealthScoreDto(
            h.Subsystem,
            h.Subsystem.ToString(),
            h.HealthScore,
            h.Trend,
            h.Trend.ToString(),
            h.AnomalyCount,
            h.EstimatedRulDays,
            h.DiagnosticsNotes,
            h.LastEvaluatedAtUtc);
    }

    private static PredictiveAlertDto MapAlertToDto(PredictiveMaintenanceAlert a)
    {
        return new PredictiveAlertDto(
            a.Id,
            a.VehicleId,
            a.Vehicle?.VehicleNumber ?? "UNKNOWN",
            a.Vehicle?.DisplayName,
            a.Subsystem,
            a.Subsystem.ToString(),
            a.RiskLevel,
            a.RiskLevel.ToString(),
            a.FailureProbability,
            a.EstimatedRulDays,
            a.ComponentTitle,
            a.SymptomDescription,
            a.RecommendedAction,
            a.EstimatedRepairCost,
            a.Currency,
            a.Status,
            a.Status.ToString(),
            a.DetectedAtUtc,
            a.PromotedMaintenanceRecordId,
            a.DismissReason);
    }

    private static string? GenerateDiagnosticNote(SubsystemCategory category, decimal score)
    {
        if (score >= 85m) return "All telemetry metrics well within OEM tolerances.";
        return category switch
        {
            SubsystemCategory.PowertrainEngine => "Elevated coolant temperature gradient and transient oil pressure dips observed under high load.",
            SubsystemCategory.ElectricalBattery => "Alternator diode ripple detected with cranking voltage sag below 9.5V during ignition cycle.",
            SubsystemCategory.BrakingChassis => "Cumulative deceleration energy indicates brake friction lining wear exceeds 75% threshold.",
            SubsystemCategory.TransmissionDrivetrain => "Hydraulic line pressure variance detected during high torque gear shifts.",
            SubsystemCategory.TiresSuspension => "3-axis accelerometer shows elevated vibration frequencies corresponding to tire uneven wear.",
            _ => "Telemetry pattern deviation detected."
        };
    }

    private static (string Title, string Symptom, string Action, decimal Cost) GetRecommendation(SubsystemCategory category, decimal score)
    {
        return category switch
        {
            SubsystemCategory.PowertrainEngine => (
                "Engine Thermostat & Cooling Degradation",
                "Coolant temperature exceeding 105°C during highway cruising; radiator fan duty cycle maxed out.",
                "Flush cooling system, pressure test radiator, and replace thermostat valve before next regional transit.",
                320m),
            SubsystemCategory.ElectricalBattery => (
                "Battery & Alternator Charging Failure",
                "Cranking voltage dropping to 9.1V on cold starts. High AC ripple indicating degraded diode pack.",
                "Perform carbon-pile battery load test; replace alternator assembly and clean main battery ground straps.",
                450m),
            SubsystemCategory.BrakingChassis => (
                "Brake Pad Lining & Rotor Wear",
                "High deceleration stress pattern with telemetry indicating estimated pad thickness below 3mm.",
                "Inspect front and rear brake pads and calipers; resurface or replace rotors to prevent backing plate contact.",
                280m),
            SubsystemCategory.TransmissionDrivetrain => (
                "Transmission Fluid Thermal Slip",
                "Torque converter slip ratio elevated by 18% with fluid operating temperature trending 12°C above baseline.",
                "Drain and inspect transmission fluid for metallic debris, replace internal filter, and inspect torque lockup solenoid.",
                620m),
            SubsystemCategory.TiresSuspension => (
                "Suspension Bushing & Tire Tread Wear",
                "High frequency chassis oscillations and mileage threshold indicating irregular tire cup wear.",
                "Perform 4-wheel laser alignment, rotate tires, and inspect front shock absorber strut mounts.",
                220m),
            _ => (
                "Component Diagnostic Warning",
                "Telemetry variance detected.",
                "Conduct manual multi-point vehicle inspection.",
                150m)
        };
    }
}

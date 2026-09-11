using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenancePlanService : IMaintenancePlanService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public MaintenancePlanService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<MaintenancePlanListItemDto>> GetAllAsync(
        bool? activeOnly = null,
        Guid? vehicleCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.MaintenancePlans
            .AsNoTracking()
            .Include(x => x.VehicleCategory)
            .Include(x => x.Rules)
            .Include(x => x.Assignments.Where(a => a.IsActive))
            .Where(x => x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        if (vehicleCategoryId.HasValue)
        {
            query = query.Where(x => x.VehicleCategoryId == null || x.VehicleCategoryId == vehicleCategoryId.Value);
        }

        var plans = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(x => new MaintenancePlanListItemDto(
            x.Id,
            x.Code,
            x.Name,
            x.Description,
            x.VehicleCategoryId,
            x.VehicleCategory?.Name,
            x.IsActive,
            x.Rules.Count(r => !r.IsDeleted),
            x.Assignments.Count(a => a.IsActive && !a.IsDeleted),
            x.CreatedAtUtc)).ToList();
    }

    public async Task<MaintenancePlanDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var plan = await _dbContext.MaintenancePlans
            .AsNoTracking()
            .Include(x => x.VehicleCategory)
            .Include(x => x.Rules.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.MaintenanceServiceType)
            .Include(x => x.Assignments.Where(a => a.IsActive && !a.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException($"Maintenance plan '{id}' was not found.");

        return MapToDto(plan);
    }

    public async Task<MaintenancePlanDto> CreateAsync(
        CreateMaintenancePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.MaintenancePlans
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance plan with code '{normalizedCode}' already exists for this tenant.");

        if (request.VehicleCategoryId.HasValue)
        {
            var catExists = await _dbContext.VehicleCategories
                .AnyAsync(x => x.Id == request.VehicleCategoryId.Value && x.TenantId == tenantId, cancellationToken);

            if (!catExists)
                throw new KeyNotFoundException($"Vehicle category '{request.VehicleCategoryId.Value}' was not found.");
        }

        var plan = new MaintenancePlan(
            tenantId,
            normalizedCode,
            request.Name,
            request.Description,
            request.VehicleCategoryId,
            request.IsActive);

        _dbContext.MaintenancePlans.Add(plan);

        if (request.Rules is not null && request.Rules.Count > 0)
        {
            foreach (var ruleReq in request.Rules)
            {
                var serviceTypeExists = await _dbContext.MaintenanceServiceTypes
                    .AnyAsync(x => x.Id == ruleReq.MaintenanceServiceTypeId && x.TenantId == tenantId, cancellationToken);

                if (!serviceTypeExists)
                    throw new KeyNotFoundException($"Maintenance service type '{ruleReq.MaintenanceServiceTypeId}' was not found.");

                var rule = new MaintenancePlanRule(
                    tenantId,
                    plan.Id,
                    ruleReq.MaintenanceServiceTypeId,
                    ruleReq.ScheduleType,
                    ruleReq.IntervalKilometers,
                    ruleReq.IntervalMiles,
                    ruleReq.IntervalEngineHours,
                    ruleReq.IntervalDays,
                    ruleReq.IntervalMonths,
                    ruleReq.InitialDueKilometers,
                    ruleReq.InitialDueEngineHours,
                    ruleReq.InitialDueDateUtc,
                    ruleReq.ReminderBeforeKilometers,
                    ruleReq.ReminderBeforeEngineHours,
                    ruleReq.ReminderBeforeDays,
                    ruleReq.ToleranceKilometers,
                    ruleReq.ToleranceHours,
                    ruleReq.ToleranceDays,
                    ruleReq.IsMandatory,
                    ruleReq.IsActive,
                    ruleReq.Notes);

                _dbContext.MaintenancePlanRules.Add(rule);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePlanCreated,
                "MaintenancePlan",
                plan.Id.ToString(),
                $"Created maintenance plan '{plan.Name}' ({plan.Code})"),
            cancellationToken);

        return await GetByIdAsync(plan.Id, cancellationToken);
    }

    public async Task<MaintenancePlanDto> UpdateAsync(
        Guid id,
        UpdateMaintenancePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var plan = await _dbContext.MaintenancePlans
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException($"Maintenance plan '{id}' was not found.");

        var codeExists = await _dbContext.MaintenancePlans
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance plan with code '{normalizedCode}' already exists for this tenant.");

        if (request.VehicleCategoryId.HasValue)
        {
            var catExists = await _dbContext.VehicleCategories
                .AnyAsync(x => x.Id == request.VehicleCategoryId.Value && x.TenantId == tenantId, cancellationToken);

            if (!catExists)
                throw new KeyNotFoundException($"Vehicle category '{request.VehicleCategoryId.Value}' was not found.");
        }

        plan.Update(
            normalizedCode,
            request.Name,
            request.Description,
            request.VehicleCategoryId,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePlanUpdated,
                "MaintenancePlan",
                plan.Id.ToString(),
                $"Updated maintenance plan '{plan.Name}' ({plan.Code})"),
            cancellationToken);

        return await GetByIdAsync(plan.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var plan = await _dbContext.MaintenancePlans
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException($"Maintenance plan '{id}' was not found.");

        plan.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePlanDeleted,
                "MaintenancePlan",
                plan.Id.ToString(),
                $"Deleted maintenance plan '{plan.Name}' ({plan.Code})"),
            cancellationToken);
    }

    public async Task<MaintenancePlanRuleDto> AddRuleAsync(
        Guid planId,
        CreateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var plan = await _dbContext.MaintenancePlans
            .FirstOrDefaultAsync(x => x.Id == planId && x.TenantId == tenantId, cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException($"Maintenance plan '{planId}' was not found.");

        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(x => x.Id == request.MaintenanceServiceTypeId && x.TenantId == tenantId, cancellationToken);

        if (serviceType is null)
            throw new KeyNotFoundException($"Maintenance service type '{request.MaintenanceServiceTypeId}' was not found.");

        var rule = new MaintenancePlanRule(
            tenantId,
            planId,
            request.MaintenanceServiceTypeId,
            request.ScheduleType,
            request.IntervalKilometers,
            request.IntervalMiles,
            request.IntervalEngineHours,
            request.IntervalDays,
            request.IntervalMonths,
            request.InitialDueKilometers,
            request.InitialDueEngineHours,
            request.InitialDueDateUtc,
            request.ReminderBeforeKilometers,
            request.ReminderBeforeEngineHours,
            request.ReminderBeforeDays,
            request.ToleranceKilometers,
            request.ToleranceHours,
            request.ToleranceDays,
            request.IsMandatory,
            request.IsActive,
            request.Notes);

        _dbContext.MaintenancePlanRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToRuleDto(rule, serviceType);
    }

    public async Task<MaintenancePlanRuleDto> UpdateRuleAsync(
        Guid planId,
        Guid ruleId,
        UpdateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var rule = await _dbContext.MaintenancePlanRules
            .Include(x => x.MaintenanceServiceType)
            .FirstOrDefaultAsync(x => x.Id == ruleId && x.MaintenancePlanId == planId && x.TenantId == tenantId, cancellationToken);

        if (rule is null)
            throw new KeyNotFoundException($"Maintenance plan rule '{ruleId}' was not found for plan '{planId}'.");

        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(x => x.Id == request.MaintenanceServiceTypeId && x.TenantId == tenantId, cancellationToken);

        if (serviceType is null)
            throw new KeyNotFoundException($"Maintenance service type '{request.MaintenanceServiceTypeId}' was not found.");

        rule.Update(
            request.MaintenanceServiceTypeId,
            request.ScheduleType,
            request.IntervalKilometers,
            request.IntervalMiles,
            request.IntervalEngineHours,
            request.IntervalDays,
            request.IntervalMonths,
            request.InitialDueKilometers,
            request.InitialDueEngineHours,
            request.InitialDueDateUtc,
            request.ReminderBeforeKilometers,
            request.ReminderBeforeEngineHours,
            request.ReminderBeforeDays,
            request.ToleranceKilometers,
            request.ToleranceHours,
            request.ToleranceDays,
            request.IsMandatory,
            request.IsActive,
            request.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToRuleDto(rule, serviceType);
    }

    public async Task DeleteRuleAsync(
        Guid planId,
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var rule = await _dbContext.MaintenancePlanRules
            .FirstOrDefaultAsync(x => x.Id == ruleId && x.MaintenancePlanId == planId && x.TenantId == tenantId, cancellationToken);

        if (rule is null)
            throw new KeyNotFoundException($"Maintenance plan rule '{ruleId}' was not found for plan '{planId}'.");

        rule.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehicleMaintenancePlanAssignmentDto>> GetVehicleAssignmentsAsync(
        Guid? vehicleId = null,
        Guid? planId = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleMaintenancePlanAssignments
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenancePlan)
            .Where(x => x.TenantId == tenantId);

        if (vehicleId.HasValue)
            query = query.Where(x => x.VehicleId == vehicleId.Value);

        if (planId.HasValue)
            query = query.Where(x => x.MaintenancePlanId == planId.Value);

        if (activeOnly.HasValue && activeOnly.Value)
            query = query.Where(x => x.IsActive);

        var list = await query
            .OrderByDescending(x => x.EffectiveFromUtc)
            .ToListAsync(cancellationToken);

        return list.Select(x => new VehicleMaintenancePlanAssignmentDto(
            x.Id,
            x.TenantId,
            x.VehicleId,
            x.Vehicle.VehicleNumber,
            x.Vehicle.RegistrationNumber,
            x.Vehicle.DisplayName,
            x.MaintenancePlanId,
            x.MaintenancePlan.Code,
            x.MaintenancePlan.Name,
            x.EffectiveFromUtc,
            x.EffectiveToUtc,
            x.BaselineOdometer,
            x.BaselineEngineHours,
            x.IsActive,
            x.AssignedByUserId,
            x.Notes,
            x.CreatedAtUtc)).ToList();
    }

    public async Task<VehicleMaintenancePlanAssignmentDto> AssignPlanToVehicleAsync(
        AssignVehiclePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        var plan = await _dbContext.MaintenancePlans
            .FirstOrDefaultAsync(x => x.Id == request.MaintenancePlanId && x.TenantId == tenantId, cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException($"Maintenance plan '{request.MaintenancePlanId}' was not found.");

        // Check duplicate active assignment of the same plan
        var duplicateAssignment = await _dbContext.VehicleMaintenancePlanAssignments
            .AnyAsync(x => x.TenantId == tenantId && x.VehicleId == request.VehicleId && x.MaintenancePlanId == request.MaintenancePlanId && x.IsActive, cancellationToken);

        if (duplicateAssignment)
            throw new ConflictException($"Maintenance plan '{plan.Name}' is already actively assigned to vehicle '{vehicle.VehicleNumber}'.");

        var baselineOdometer = request.BaselineOdometer ?? vehicle.CurrentOdometer;

        var assignment = new VehicleMaintenancePlanAssignment(
            tenantId,
            request.VehicleId,
            request.MaintenancePlanId,
            request.EffectiveFromUtc,
            request.EffectiveToUtc,
            baselineOdometer,
            request.BaselineEngineHours,
            _currentUserContext.UserId,
            request.Notes,
            isActive: true);

        _dbContext.VehicleMaintenancePlanAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePlanAssigned,
                "VehicleMaintenancePlanAssignment",
                assignment.Id.ToString(),
                $"Assigned maintenance plan '{plan.Name}' to vehicle '{vehicle.VehicleNumber}'"),
            cancellationToken);

        return new VehicleMaintenancePlanAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.VehicleId,
            vehicle.VehicleNumber,
            vehicle.RegistrationNumber,
            vehicle.DisplayName,
            assignment.MaintenancePlanId,
            plan.Code,
            plan.Name,
            assignment.EffectiveFromUtc,
            assignment.EffectiveToUtc,
            assignment.BaselineOdometer,
            assignment.BaselineEngineHours,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task<VehicleMaintenancePlanAssignmentDto> UpdateVehicleAssignmentAsync(
        Guid assignmentId,
        UpdateVehiclePlanAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.VehicleMaintenancePlanAssignments
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenancePlan)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Vehicle plan assignment '{assignmentId}' was not found.");

        assignment.Update(
            request.EffectiveFromUtc,
            request.EffectiveToUtc,
            request.BaselineOdometer,
            request.BaselineEngineHours,
            request.Notes,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VehicleMaintenancePlanAssignmentDto(
            assignment.Id,
            assignment.TenantId,
            assignment.VehicleId,
            assignment.Vehicle.VehicleNumber,
            assignment.Vehicle.RegistrationNumber,
            assignment.Vehicle.DisplayName,
            assignment.MaintenancePlanId,
            assignment.MaintenancePlan.Code,
            assignment.MaintenancePlan.Name,
            assignment.EffectiveFromUtc,
            assignment.EffectiveToUtc,
            assignment.BaselineOdometer,
            assignment.BaselineEngineHours,
            assignment.IsActive,
            assignment.AssignedByUserId,
            assignment.Notes,
            assignment.CreatedAtUtc);
    }

    public async Task RemoveVehicleAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var assignment = await _dbContext.VehicleMaintenancePlanAssignments
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenancePlan)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.TenantId == tenantId, cancellationToken);

        if (assignment is null)
            throw new KeyNotFoundException($"Vehicle plan assignment '{assignmentId}' was not found.");

        assignment.Deactivate(DateTime.UtcNow, _currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePlanUnassigned,
                "VehicleMaintenancePlanAssignment",
                assignment.Id.ToString(),
                $"Unassigned maintenance plan '{assignment.MaintenancePlan.Name}' from vehicle '{assignment.Vehicle.VehicleNumber}'"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static MaintenancePlanDto MapToDto(MaintenancePlan entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.VehicleCategoryId,
            entity.VehicleCategory?.Name,
            entity.IsActive,
            entity.Rules.Count(r => !r.IsDeleted),
            entity.Assignments.Count(a => a.IsActive && !a.IsDeleted),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Rules.Where(r => !r.IsDeleted).Select(r => MapToRuleDto(r, r.MaintenanceServiceType)).ToList());

    private static MaintenancePlanRuleDto MapToRuleDto(MaintenancePlanRule rule, MaintenanceServiceType? serviceType) =>
        new(
            rule.Id,
            rule.MaintenancePlanId,
            rule.MaintenanceServiceTypeId,
            serviceType?.Code ?? string.Empty,
            serviceType?.Name ?? string.Empty,
            serviceType?.Category ?? MaintenanceServiceCategory.Preventive,
            serviceType?.Category.ToString() ?? MaintenanceServiceCategory.Preventive.ToString(),
            rule.ScheduleType,
            rule.ScheduleType.ToString(),
            rule.IntervalKilometers,
            rule.IntervalMiles,
            rule.IntervalEngineHours,
            rule.IntervalDays,
            rule.IntervalMonths,
            rule.InitialDueKilometers,
            rule.InitialDueEngineHours,
            rule.InitialDueDateUtc,
            rule.ReminderBeforeKilometers,
            rule.ReminderBeforeEngineHours,
            rule.ReminderBeforeDays,
            rule.ToleranceKilometers,
            rule.ToleranceHours,
            rule.ToleranceDays,
            rule.IsMandatory,
            rule.IsActive,
            rule.Notes);
}

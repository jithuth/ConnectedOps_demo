namespace ConnectedOps.Application.Maintenance;

public interface IMaintenancePlanService
{
    Task<IReadOnlyCollection<MaintenancePlanListItemDto>> GetAllAsync(
        bool? activeOnly = null,
        Guid? vehicleCategoryId = null,
        CancellationToken cancellationToken = default);

    Task<MaintenancePlanDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MaintenancePlanDto> CreateAsync(
        CreateMaintenancePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenancePlanDto> UpdateAsync(
        Guid id,
        UpdateMaintenancePlanRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Plan Rules
    Task<MaintenancePlanRuleDto> AddRuleAsync(
        Guid planId,
        CreateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenancePlanRuleDto> UpdateRuleAsync(
        Guid planId,
        Guid ruleId,
        UpdateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteRuleAsync(
        Guid planId,
        Guid ruleId,
        CancellationToken cancellationToken = default);

    // Vehicle Assignments
    Task<IReadOnlyCollection<VehicleMaintenancePlanAssignmentDto>> GetVehicleAssignmentsAsync(
        Guid? vehicleId = null,
        Guid? planId = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenancePlanAssignmentDto> AssignPlanToVehicleAsync(
        AssignVehiclePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenancePlanAssignmentDto> UpdateVehicleAssignmentAsync(
        Guid assignmentId,
        UpdateVehiclePlanAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveVehicleAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}

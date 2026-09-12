using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Safety;

public interface ISafetyViolationService
{
    Task<PagedResult<SafetyViolationDto>> GetPagedAsync(SafetyViolationFilterRequest filter, CancellationToken cancellationToken = default);
    Task<SafetyViolationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SafetyViolationDto> CreateAsync(CreateSafetyViolationRequest request, CancellationToken cancellationToken = default);
    Task<SafetyViolationDto> UpdateAsync(Guid id, UpdateSafetyViolationRequest request, CancellationToken cancellationToken = default);
    Task<SafetyViolationDto> ResolveAsync(Guid id, ResolveSafetyViolationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SafetyViolationDto>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}

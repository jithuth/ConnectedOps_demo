using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Safety;

public interface ICorrectiveActionService
{
    Task<PagedResult<CorrectiveActionDto>> GetPagedAsync(CorrectiveActionFilterRequest filter, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> CreateAsync(CreateCorrectiveActionRequest request, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> UpdateAsync(Guid id, UpdateCorrectiveActionRequest request, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> CompleteAsync(Guid id, CompleteCorrectiveActionRequest request, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> VerifyAsync(Guid id, VerifyCorrectiveActionRequest request, CancellationToken cancellationToken = default);
    Task<CorrectiveActionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CorrectiveActionDto>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}

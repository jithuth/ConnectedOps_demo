using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Safety;

public interface ISafetyIncidentService
{
    Task<PagedResult<SafetyIncidentDto>> GetPagedAsync(SafetyIncidentFilterRequest filter, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> CreateAsync(CreateSafetyIncidentRequest request, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> UpdateAsync(Guid id, UpdateSafetyIncidentRequest request, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> StartInvestigationAsync(Guid id, StartSafetyInvestigationRequest request, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> CompleteInvestigationAsync(Guid id, CompleteSafetyInvestigationRequest request, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> CloseAsync(Guid id, CloseSafetyIncidentRequest request, CancellationToken cancellationToken = default);
    Task<SafetyIncidentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SafetyIncidentParticipantDto> AddParticipantAsync(Guid incidentId, AddIncidentParticipantRequest request, CancellationToken cancellationToken = default);
    Task RemoveParticipantAsync(Guid incidentId, Guid participantId, CancellationToken cancellationToken = default);
    Task<SafetyIncidentVehicleDto> AddVehicleAsync(Guid incidentId, AddIncidentVehicleRequest request, CancellationToken cancellationToken = default);
    Task RemoveVehicleAsync(Guid incidentId, Guid incidentVehicleId, CancellationToken cancellationToken = default);
    Task<SafetyIncidentAssetDto> AddAssetAsync(Guid incidentId, AddIncidentAssetRequest request, CancellationToken cancellationToken = default);
    Task RemoveAssetAsync(Guid incidentId, Guid incidentAssetId, CancellationToken cancellationToken = default);
    Task<SafetyIncidentEvidenceDto> AddEvidenceAsync(Guid incidentId, AddIncidentEvidenceRequest request, CancellationToken cancellationToken = default);
    Task RemoveEvidenceAsync(Guid incidentId, Guid evidenceId, CancellationToken cancellationToken = default);
}

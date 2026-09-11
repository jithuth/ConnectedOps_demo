namespace ConnectedOps.Application.Vehicles;

public interface IVehicleDocumentService
{
    Task<IReadOnlyCollection<VehicleDocumentDto>> GetDocumentsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<VehicleDocumentDto> GetDocumentByIdAsync(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<VehicleDocumentDto> AddDocumentAsync(
        Guid vehicleId,
        CreateVehicleDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleDocumentDto> UpdateDocumentAsync(
        Guid vehicleId,
        Guid documentId,
        UpdateVehicleDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExpiringVehicleDocumentAlertDto>> GetExpiringDocumentsAsync(
        int daysThreshold = 30,
        CancellationToken cancellationToken = default);
}

namespace ConnectedOps.Application.Drivers;

public interface IDriverDocumentService
{
    Task<IReadOnlyCollection<DriverDocumentDto>> GetDocumentsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<DriverDocumentDto> GetDocumentByIdAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<DriverDocumentDto> AddDocumentAsync(
        Guid driverId,
        CreateDriverDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDocumentDto> UpdateDocumentAsync(
        Guid driverId,
        Guid documentId,
        UpdateDriverDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateDocumentAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}

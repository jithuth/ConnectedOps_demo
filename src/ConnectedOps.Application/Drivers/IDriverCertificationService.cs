namespace ConnectedOps.Application.Drivers;

public interface IDriverCertificationService
{
    Task<IReadOnlyCollection<DriverCertificationDto>> GetCertificationsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<DriverCertificationDto> GetCertificationByIdAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default);

    Task<DriverCertificationDto> AddCertificationAsync(
        Guid driverId,
        CreateDriverCertificationRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverCertificationDto> UpdateCertificationAsync(
        Guid driverId,
        Guid certificationId,
        UpdateDriverCertificationRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateCertificationAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default);

    Task DeleteCertificationAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default);
}

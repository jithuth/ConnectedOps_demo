namespace ConnectedOps.Application.Drivers;

public interface IDriverLicenseService
{
    Task<IReadOnlyCollection<DriverLicenseDto>> GetLicensesAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseDto> GetLicenseByIdAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseDto> AddLicenseAsync(
        Guid driverId,
        CreateDriverLicenseRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseDto> UpdateLicenseAsync(
        Guid driverId,
        Guid licenseId,
        UpdateDriverLicenseRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseDto> SetPrimaryLicenseAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default);

    Task DeactivateLicenseAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseCategoryDto> AddCategoryAsync(
        Guid driverId,
        Guid licenseId,
        AddDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverLicenseCategoryDto> UpdateCategoryAsync(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        UpdateDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Drivers;

public interface IDriverService
{
    Task<PagedResult<DriverListItemDto>> GetDriversPagedAsync(
        DriverQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> GetDriverByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> CreateDriverAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> UpdateDriverAsync(
        Guid id,
        UpdateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeDriverStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> LinkEmployeeAsync(
        Guid id,
        LinkDriverEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> AssignBranchAsync(
        Guid id,
        AssignDriverBranchRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverDetailDto> AssignDepartmentAsync(
        Guid id,
        AssignDriverDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverEmergencyContactDto> AddEmergencyContactAsync(
        Guid driverId,
        CreateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverEmergencyContactDto> UpdateEmergencyContactAsync(
        Guid driverId,
        Guid contactId,
        UpdateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteEmergencyContactAsync(
        Guid driverId,
        Guid contactId,
        CancellationToken cancellationToken = default);

    Task<DriverNoteDto> AddNoteAsync(
        Guid driverId,
        CreateDriverNoteRequest request,
        CancellationToken cancellationToken = default);

    Task SetProfileImageAsync(
        Guid driverId,
        string? objectKey,
        CancellationToken cancellationToken = default);

    Task DeleteDriverAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

namespace ConnectedOps.Application.Organization;

public interface IDepartmentService
{
    Task<IReadOnlyCollection<DepartmentListItemDto>> GetDepartmentsAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

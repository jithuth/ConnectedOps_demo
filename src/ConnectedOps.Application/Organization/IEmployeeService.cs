namespace ConnectedOps.Application.Organization;

public interface IEmployeeService
{
    Task<IReadOnlyCollection<EmployeeListItemDto>> GetEmployeesAsync(
        Guid? branchId = null,
        Guid? departmentId = null,
        Guid? teamId = null,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> UpdateEmployeeAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteEmployeeAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task LinkUserAsync(
        Guid employeeId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UnlinkUserAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

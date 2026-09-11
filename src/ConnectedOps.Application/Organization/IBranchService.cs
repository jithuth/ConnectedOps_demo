namespace ConnectedOps.Application.Organization;

public interface IBranchService
{
    Task<IReadOnlyCollection<BranchListItemDto>> GetBranchesAsync(
        CancellationToken cancellationToken = default);

    Task<BranchDto> GetBranchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BranchDto> CreateBranchAsync(
        CreateBranchRequest request,
        CancellationToken cancellationToken = default);

    Task<BranchDto> UpdateBranchAsync(
        Guid id,
        UpdateBranchRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteBranchAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

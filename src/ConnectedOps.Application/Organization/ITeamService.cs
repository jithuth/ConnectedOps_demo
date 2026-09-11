namespace ConnectedOps.Application.Organization;

public interface ITeamService
{
    Task<IReadOnlyCollection<TeamListItemDto>> GetTeamsAsync(
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<TeamDto> GetTeamByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TeamDto> CreateTeamAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<TeamDto> UpdateTeamAsync(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteTeamAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Gamification;

public interface IGamificationService
{
    Task<FleetLeaderboardDto> GetLeaderboardAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    Task<PagedResult<DriverScorecardDto>> GetScorecardsPagedAsync(ScorecardFilterRequest request, CancellationToken cancellationToken = default);

    Task<DriverScorecardDto?> GetDriverScorecardAsync(Guid driverId, int? month = null, int? year = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DriverBadgeDto>> GetDriverBadgesAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<DriverBadgeDto> AwardBadgeAsync(AwardBadgeRequest request, CancellationToken cancellationToken = default);

    Task<int> RecalculateAllScorecardsAsync(int month, int year, CancellationToken cancellationToken = default);
}

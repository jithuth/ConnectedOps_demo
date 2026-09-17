using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Gamification;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Gamification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Gamification;

[Authorize]
public class LeaderboardModel : PageModel
{
    private readonly IGamificationService _gamificationService;
    private readonly IDriverService _driverService;

    public LeaderboardModel(
        IGamificationService gamificationService,
        IDriverService driverService)
    {
        _gamificationService = gamificationService;
        _driverService = driverService;
    }

    public FleetLeaderboardDto Leaderboard { get; private set; } = null!;
    public PagedResult<DriverScorecardDto> Scorecards { get; private set; } = null!;
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public DriverTier? TierFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var targetYear = Year ?? DateTime.UtcNow.Year;
        var targetMonth = Month ?? DateTime.UtcNow.Month;
        Month = targetMonth;
        Year = targetYear;

        Leaderboard = await _gamificationService.GetLeaderboardAsync(targetMonth, targetYear, cancellationToken);

        Scorecards = await _gamificationService.GetScorecardsPagedAsync(
            new ScorecardFilterRequest(
                Month: targetMonth,
                Year: targetYear,
                Tier: TierFilter,
                PageNumber: PageNumber,
                PageSize: 20),
            cancellationToken);

        var driversPaged = await _driverService.GetDriversPagedAsync(
            new DriverQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Drivers = driversPaged.Items;
    }

    public async Task<IActionResult> OnPostRecalculateAsync(int targetMonth, int targetYear, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _gamificationService.RecalculateAllScorecardsAsync(targetMonth, targetYear, cancellationToken);
            SuccessMessage = $"Scorecards recalculated successfully for {count} drivers.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Month = targetMonth, Year = targetYear });
    }

    public async Task<IActionResult> OnPostAwardBadgeAsync(
        Guid driverId,
        BadgeType badgeType,
        string title,
        string description,
        int points,
        CancellationToken cancellationToken)
    {
        try
        {
            await _gamificationService.AwardBadgeAsync(
                new AwardBadgeRequest(driverId, badgeType, title, description, points),
                cancellationToken);
            SuccessMessage = $"Badge '{title}' successfully awarded (+{points} points)!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Month, Year });
    }
}

using ConnectedOps.Application.Gamification;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Gamification;

[ApiController]
[Route("api/gamification")]
[Authorize]
public sealed class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;

    public GamificationController(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    [HttpGet("leaderboard")]
    [RequirePermission(PermissionKeys.Gamification.ViewLeaderboard)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _gamificationService.GetLeaderboardAsync(month, year, cancellationToken);
        return Ok(result);
    }

    [HttpGet("scorecards")]
    [RequirePermission(PermissionKeys.Gamification.ViewScorecards)]
    public async Task<IActionResult> GetScorecards([FromQuery] ScorecardFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _gamificationService.GetScorecardsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("scorecards/{driverId:guid}")]
    [RequirePermission(PermissionKeys.Gamification.ViewScorecards)]
    public async Task<IActionResult> GetDriverScorecard(Guid driverId, [FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _gamificationService.GetDriverScorecardAsync(driverId, month, year, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("badges/{driverId:guid}")]
    [RequirePermission(PermissionKeys.Gamification.ViewScorecards)]
    public async Task<IActionResult> GetDriverBadges(Guid driverId, CancellationToken cancellationToken)
    {
        var result = await _gamificationService.GetDriverBadgesAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("badges")]
    [RequirePermission(PermissionKeys.Gamification.ManageBadges)]
    public async Task<IActionResult> AwardBadge([FromBody] AwardBadgeRequest request, CancellationToken cancellationToken)
    {
        var result = await _gamificationService.AwardBadgeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("recalculate")]
    [RequirePermission(PermissionKeys.Gamification.ManageBadges)]
    public async Task<IActionResult> RecalculateAll([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var processed = await _gamificationService.RecalculateAllScorecardsAsync(month, year, cancellationToken);
        return Ok(new { processedDrivers = processed, month, year });
    }
}

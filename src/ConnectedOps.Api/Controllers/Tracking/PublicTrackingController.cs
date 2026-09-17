using ConnectedOps.Application.Tracking;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Tracking;

[ApiController]
[Route("api/tracking")]
public sealed class PublicTrackingController : ControllerBase
{
    private readonly IPublicTrackingService _trackingService;

    public PublicTrackingController(IPublicTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpPost("generate-token")]
    [Authorize]
    [RequirePermission(PermissionKeys.Dispatch.ViewJobs)]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTrackingTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _trackingService.GenerateTokenForJobAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("public/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicTracking(string token, CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetPublicTrackingInfoAsync(token, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Tracking link is invalid or has expired." });
        }
        return Ok(result);
    }

    [HttpPost("public/{token}/rating")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitRating(string token, [FromBody] SubmitDeliveryRatingRequest request, CancellationToken cancellationToken)
    {
        var success = await _trackingService.SubmitDeliveryRatingAsync(token, request, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Tracking link is invalid or has expired." });
        }
        return Ok(new { success = true, message = "Thank you for your feedback!" });
    }
}

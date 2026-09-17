using ConnectedOps.Application.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Tracking;

[AllowAnonymous]
public class LiveModel : PageModel
{
    private readonly IPublicTrackingService _trackingService;

    public LiveModel(IPublicTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public PublicTrackingInfoDto? TrackingInfo { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return Page();
        }

        TrackingInfo = await _trackingService.GetPublicTrackingInfoAsync(Token, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRatingAsync(int rating, string? feedbackComment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return RedirectToPage();
        }

        try
        {
            var success = await _trackingService.SubmitDeliveryRatingAsync(
                Token,
                new SubmitDeliveryRatingRequest(rating, feedbackComment),
                cancellationToken);

            if (success)
            {
                SuccessMessage = "Thank you! Your delivery rating and feedback have been submitted.";
            }
            else
            {
                ErrorMessage = "Unable to submit feedback. The tracking link may have expired.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Token });
    }
}

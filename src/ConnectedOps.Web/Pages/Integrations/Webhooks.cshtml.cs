using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Integrations;

[Authorize]
public class WebhooksModel : PageModel
{
    private readonly IWebhookService _webhookService;

    public WebhooksModel(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    public IReadOnlyCollection<WebhookSubscriptionDto> Subscriptions { get; private set; } = [];
    public PagedResult<WebhookDeliveryAttemptDto> DeliveryAttempts { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedSubscriptionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Subscriptions = await _webhookService.GetSubscriptionsAsync(cancellationToken);

        DeliveryAttempts = await _webhookService.GetDeliveryAttemptsPagedAsync(
            SelectedSubscriptionId,
            PageNumber,
            pageSize: 15,
            cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string url,
        string description,
        List<WebhookEventType> events,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                ErrorMessage = "Webhook endpoint URL is required.";
                return RedirectToPage();
            }

            if (events == null || events.Count == 0)
            {
                ErrorMessage = "Please select at least one subscribed event.";
                return RedirectToPage();
            }

            await _webhookService.CreateSubscriptionAsync(
                new CreateWebhookRequest(url, description, events),
                cancellationToken);

            SuccessMessage = "Webhook subscription registered successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid subscriptionId, bool isEnabled, CancellationToken cancellationToken)
    {
        try
        {
            await _webhookService.ToggleSubscriptionAsync(subscriptionId, isEnabled, cancellationToken);
            SuccessMessage = isEnabled ? "Webhook enabled." : "Webhook paused.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            await _webhookService.DeleteSubscriptionAsync(subscriptionId, cancellationToken);
            SuccessMessage = "Webhook subscription removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPingAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var attempt = await _webhookService.TriggerTestPingAsync(subscriptionId, cancellationToken);
            if (attempt.Success)
            {
                SuccessMessage = $"Ping successful! HTTP {attempt.ResponseStatusCode} in {attempt.DurationMs}ms.";
            }
            else
            {
                ErrorMessage = $"Ping failed: HTTP {attempt.ResponseStatusCode} ({attempt.ErrorMessage ?? "Connection refused"}).";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}

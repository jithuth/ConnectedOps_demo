using ConnectedOps.Application.Alerts;
using ConnectedOps.Domain.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Alerts;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IAlertService _alertService;

    public DashboardModel(IAlertService alertService)
    {
        _alertService = alertService;
    }

    public AlertDashboardDto Metrics { get; private set; } = null!;
    public IReadOnlyCollection<AlertRuleDto> Rules { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Metrics = await _alertService.GetDashboardAsync(cancellationToken);
        Rules = await _alertService.GetRulesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(Guid alertId, CancellationToken cancellationToken)
    {
        try
        {
            await _alertService.AcknowledgeAlertAsync(alertId, cancellationToken);
            SuccessMessage = "Alert acknowledged successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid alertId, string? notes, CancellationToken cancellationToken)
    {
        try
        {
            await _alertService.ResolveAlertAsync(alertId, notes, cancellationToken);
            SuccessMessage = "Alert resolved successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendNotificationAsync(
        Guid? alertId,
        NotificationChannelType channel,
        string recipient,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        try
        {
            await _alertService.SendNotificationAsync(
                new SendNotificationRequest(channel, recipient, title, body, alertId),
                cancellationToken);

            SuccessMessage = $"Alert notification sent via {channel} to {recipient}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateRuleAsync(
        string code,
        string name,
        AlertSourceType sourceType,
        AlertSeverity severity,
        int cooldownMinutes,
        string? description,
        string? fieldName,
        ConditionOperator? conditionOperator,
        string? thresholdValue,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = new List<CreateAlertRuleConditionRequest>();
            if (!string.IsNullOrWhiteSpace(fieldName) && conditionOperator.HasValue && !string.IsNullOrWhiteSpace(thresholdValue))
            {
                conditions.Add(new CreateAlertRuleConditionRequest(fieldName, conditionOperator.Value, thresholdValue));
            }

            await _alertService.CreateRuleAsync(
                new CreateAlertRuleRequest(code, name, sourceType, severity, cooldownMinutes, description, conditions),
                cancellationToken);

            SuccessMessage = $"Alert rule '{name}' configured successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}

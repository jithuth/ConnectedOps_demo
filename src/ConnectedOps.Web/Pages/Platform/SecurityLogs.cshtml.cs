using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class SecurityLogsModel : PageModel
{
    private readonly IPlatformSecurityService _securityService;

    public SecurityLogsModel(IPlatformSecurityService securityService)
    {
        _securityService = securityService;
    }

    public SecurityLogPage SecurityPage { get; private set; } = null!;
    public PlatformSecurityDashboardStatsDto Stats { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public DateTime? FromUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? TenantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? IpAddress { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EventType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Succeeded { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Stats = await _securityService.GetSecurityDashboardStatsAsync(HttpContext.RequestAborted);

        var query = new PlatformSecurityLogQuery(
            FromUtc: FromUtc,
            ToUtc: ToUtc,
            TenantId: TenantId,
            Email: Email,
            IpAddress: IpAddress,
            EventType: EventType,
            Succeeded: Succeeded,
            Page: PageIndex,
            PageSize: 20);

        SecurityPage = await _securityService.GetSecurityLogsAsync(query, HttpContext.RequestAborted);
    }
}

using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class DashboardModel : PageModel
{
    private readonly IPlatformDashboardService _dashboardService;
    private readonly IPlatformSecurityService _securityService;
    private readonly IPlatformAuditService _auditService;

    public DashboardModel(
        IPlatformDashboardService dashboardService,
        IPlatformSecurityService securityService,
        IPlatformAuditService auditService)
    {
        _dashboardService = dashboardService;
        _securityService = securityService;
        _auditService = auditService;
    }

    public PlatformDashboardStatsDto Stats { get; private set; } = null!;
    public PlatformSecurityDashboardStatsDto SecurityStats { get; private set; } = null!;
    public IReadOnlyCollection<SecurityLogDto> RecentSecurityLogs { get; private set; } = [];
    public IReadOnlyCollection<AuditLogDto> RecentAuditLogs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Stats = await _dashboardService.GetDashboardStatsAsync(HttpContext.RequestAborted);
        SecurityStats = await _securityService.GetSecurityDashboardStatsAsync(HttpContext.RequestAborted);

        var securityPage = await _securityService.GetSecurityLogsAsync(
            new PlatformSecurityLogQuery(Page: 1, PageSize: 5),
            HttpContext.RequestAborted);
        RecentSecurityLogs = securityPage.Items;

        var auditPage = await _auditService.GetAuditLogsAsync(
            new PlatformAuditLogQuery(Page: 1, PageSize: 5),
            HttpContext.RequestAborted);
        RecentAuditLogs = auditPage.Items;
    }
}

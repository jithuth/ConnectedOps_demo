using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class AuditLogsModel : PageModel
{
    private readonly IPlatformAuditService _auditService;

    public AuditLogsModel(IPlatformAuditService auditService)
    {
        _auditService = auditService;
    }

    public AuditLogPage AuditPage { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public DateTime? FromUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? TenantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var query = new PlatformAuditLogQuery(
            FromUtc: FromUtc,
            ToUtc: ToUtc,
            TenantId: TenantId,
            Action: ActionName,
            EntityType: EntityType,
            EntityId: EntityId,
            Page: PageIndex,
            PageSize: 20);

        AuditPage = await _auditService.GetAuditLogsAsync(query, HttpContext.RequestAborted);
    }
}

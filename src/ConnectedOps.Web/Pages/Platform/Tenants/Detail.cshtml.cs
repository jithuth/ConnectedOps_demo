using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform.Tenants;

public sealed class DetailModel : PageModel
{
    private readonly IPlatformTenantService _tenantService;
    private readonly IPlatformAuditService _auditService;
    private readonly IPlatformSecurityService _securityService;

    public DetailModel(
        IPlatformTenantService tenantService,
        IPlatformAuditService auditService,
        IPlatformSecurityService securityService)
    {
        _tenantService = tenantService;
        _auditService = auditService;
        _securityService = securityService;
    }

    public PlatformTenantDetailDto Tenant { get; private set; } = null!;
    public PlatformTenantStatsDto Stats { get; private set; } = null!;
    public IReadOnlyCollection<PlatformTenantUserDto> Users { get; private set; } = [];
    public IReadOnlyCollection<AuditLogDto> AuditLogs { get; private set; } = [];
    public IReadOnlyCollection<SecurityLogDto> SecurityLogs { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == Guid.Empty)
            return RedirectToPage("/Platform/Tenants/Index");

        var detail = await _tenantService.GetTenantByIdAsync(Id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        Tenant = detail;
        Stats = await _tenantService.GetTenantStatisticsAsync(Id, HttpContext.RequestAborted);
        Users = await _tenantService.GetTenantUsersAsync(Id, HttpContext.RequestAborted);

        var auditPage = await _auditService.GetAuditLogsAsync(
            new PlatformAuditLogQuery(TenantId: Id, PageSize: 15),
            HttpContext.RequestAborted);
        AuditLogs = auditPage.Items;

        var securityPage = await _securityService.GetSecurityLogsAsync(
            new PlatformSecurityLogQuery(TenantId: Id, PageSize: 15),
            HttpContext.RequestAborted);
        SecurityLogs = securityPage.Items;

        return Page();
    }

    public async Task<IActionResult> OnPostActivateAsync()
    {
        await _tenantService.ActivateTenantAsync(Id, HttpContext.RequestAborted);
        TempData["Feedback"] = "Tenant activated successfully.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostSuspendAsync()
    {
        await _tenantService.SuspendTenantAsync(Id, HttpContext.RequestAborted);
        TempData["Feedback"] = "Tenant suspended successfully.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        await _tenantService.DisableTenantAsync(Id, HttpContext.RequestAborted);
        TempData["Feedback"] = "Tenant disabled successfully.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUpdateContactAsync(UpdatePlatformTenantRequest request)
    {
        try
        {
            await _tenantService.UpdateTenantAsync(Id, request, HttpContext.RequestAborted);
            TempData["Feedback"] = "Tenant contact information updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update tenant: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }
}

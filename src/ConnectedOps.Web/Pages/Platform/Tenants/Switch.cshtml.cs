using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Web.Pages.Platform.Tenants;

[Authorize]
public sealed class SwitchModel : PageModel
{
    private readonly ConnectedOpsDbContext _dbContext;

    public SwitchModel(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, string? returnUrl = null)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.Status == TenantStatus.Active, HttpContext.RequestAborted);

        if (tenant is not null)
        {
            Response.Cookies.Append("ConnectedOps.ActiveTenantId", tenant.Id.ToString(), new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Organization/Dashboard");
    }
}

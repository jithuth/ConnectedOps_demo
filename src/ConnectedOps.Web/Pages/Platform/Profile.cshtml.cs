using System.Security.Claims;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public ApplicationUser CurrentUser { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        var userIdStr = User.FindFirstValue(ConnectedOpsClaimTypes.UserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdStr, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                CurrentUser = user;
            }
        }
    }
}

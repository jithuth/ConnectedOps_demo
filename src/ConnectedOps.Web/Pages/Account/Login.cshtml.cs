using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ConnectedOps.Application.Auth;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private readonly ConnectedOps.Application.Auth.IAuthenticationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(
        ConnectedOps.Application.Auth.IAuthenticationService authService,
        UserManager<ApplicationUser> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Platform/Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var loginResult = await _authService.LoginAsync(
                new LoginRequest { Email = Email, Password = Password },
                HttpContext.RequestAborted);

            var user = await _userManager.FindByEmailAsync(Email.Trim().ToLowerInvariant());

            if (user is null || user.PlatformRole == PlatformRole.None)
            {
                ErrorMessage = "Access denied. Only platform administrators can access this portal.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName.Length > 0 ? user.FullName : user.Email ?? "Administrator"),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ConnectedOpsClaimTypes.UserId, user.Id.ToString()),
                new(ConnectedOpsClaimTypes.PlatformRole, user.PlatformRole.ToString()),
                new(ClaimTypes.Role, user.PlatformRole.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToPage("/Platform/Dashboard");
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = "An unexpected error occurred during sign in. Please try again.";
            return Page();
        }
    }
}

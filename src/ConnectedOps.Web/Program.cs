using ConnectedOps.Infrastructure;
using ConnectedOps.Infrastructure.Platform;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add Cookie Authentication for BFF web session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ConnectedOps.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Add Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Platform");
    options.Conventions.AuthorizeFolder("/Organization");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Tenant resolution & active tenant cookie propagation middleware
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userContext = context.RequestServices.GetService<ConnectedOps.Application.Common.Interfaces.ICurrentUserContext>();
        if (userContext?.TenantId is Guid tenantId)
        {
            if (!context.Request.Cookies.TryGetValue("ConnectedOps.ActiveTenantId", out var cookieVal) ||
                cookieVal != tenantId.ToString())
            {
                context.Response.Cookies.Append("ConnectedOps.ActiveTenantId", tenantId.ToString(), new CookieOptions
                {
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }
        }
    }

    await next();
});

app.MapGet("/", () => Results.Redirect("/Platform/Dashboard"));

app.MapRazorPages();

// Ensure platform superadmin and default tenant are initialized
await app.Services.BootstrapPlatformAsync();

app.Run();

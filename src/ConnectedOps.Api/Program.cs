using System.Text;

using ConnectedOps.Api.Diagnostics;
using ConnectedOps.Api.ExceptionHandling;
using ConnectedOps.Api.Filters;
using ConnectedOps.Infrastructure;
using ConnectedOps.Api.Security;
using ConnectedOps.Infrastructure.Platform;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
/// ============================================================
/// api security
/// ============================================================

builder.WebHost.ConfigureKestrel(options =>
{
    // Protect against unexpectedly large request bodies.
    //
    // Specific upload endpoints can override this later.
    options.Limits.MaxRequestBodySize =
        10 * 1024 * 1024;

    options.Limits.RequestHeadersTimeout =
        TimeSpan.FromSeconds(15);

    options.Limits.KeepAliveTimeout =
        TimeSpan.FromMinutes(2);
});

// ============================================================
// CONTROLLERS + VALIDATION
// Phase 1.18
// ============================================================

builder.Services.AddScoped<ValidationFilter>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// ============================================================
// GLOBAL EXCEPTION HANDLING
// Phase 1.17
// ============================================================

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ============================================================
// HEALTH CHECKS
// Phase 1.23
// ============================================================

builder.Services.AddAppHealthChecks();

// ============================================================
// INFRASTRUCTURE
// Phases 1.1 - 1.18
// ============================================================

builder.Services.AddInfrastructure(
    builder.Configuration);

// ============================================================
// API SECURITY HARDENING
// Phase 1.21
// ============================================================

builder.Services.AddApiSecurity();

// ============================================================
// JWT BEARER AUTHENTICATION
// Phase 1.5 + 1.8
// ============================================================

var jwtSigningKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSigningKey)),

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromSeconds(30)
            };

        // Only normal access tokens may authenticate API requests.
        // Tenant-selection tokens must not pass [Authorize].
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenType =
                    context.Principal?
                        .FindFirst("token_type")?
                        .Value;

                if (!string.Equals(
                        tokenType,
                        "access",
                        StringComparison.Ordinal))
                {
                    context.Fail(
                        "Only access tokens can be used for API authentication.");
                }

                return Task.CompletedTask;
            }
        };
    });

// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();

// ============================================================
// BUILD
// ============================================================

builder.Services.AddHsts(options =>
{
    options.Preload = true;

    options.IncludeSubDomains = true;

    options.MaxAge =
        TimeSpan.FromDays(365);
});

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseMiddleware<
    SecurityHeadersMiddleware>();

// Authentication MUST come before Authorization.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapAppHealthChecks();

await app.Services.BootstrapPlatformAsync();

app.Run();

public partial class Program { }
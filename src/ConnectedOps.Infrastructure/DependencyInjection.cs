using System.Text;

using ConnectedOps.Application.Auth;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Invitations;
using ConnectedOps.Application.Permissions;
using ConnectedOps.Application.TenantRoles;
using ConnectedOps.Application.TenantSettings;
using ConnectedOps.Application.TenantUsers;

using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Authorization;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Invitations;
using ConnectedOps.Infrastructure.Permissions;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.TenantRoles;
using ConnectedOps.Infrastructure.TenantUsers;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ApplicationAssembly =
    ConnectedOps.Application.AssemblyReference;

namespace ConnectedOps.Infrastructure;


using ConnectedOps.Application.Auditing;
using ConnectedOps.Infrastructure.Auditing;

using ConnectedOps.Application.Security;
using ConnectedOps.Infrastructure.Security;

using ConnectedOps.Application.Platform;
using ConnectedOps.Infrastructure.Platform;

using ConnectedOps.Application.Organization;
using ConnectedOps.Infrastructure.Organization;

using ConnectedOps.Application.Billing;
using ConnectedOps.Infrastructure.Billing;
using ConnectedOps.Application.Accounting;
using ConnectedOps.Infrastructure.Accounting;
using ConnectedOps.Application.Notifications;
using ConnectedOps.Infrastructure.Notifications;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Infrastructure.Vehicles;



public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ============================================================
        // PHASE 1.1 - 1.2
        // DATABASE / TENANT / IDENTITY FOUNDATION
        // ============================================================

        AddDatabase(
            services,
            configuration);

        AddIdentity(
            services);

        // ============================================================
        // PHASE 1.5 - 1.10
        // JWT / AUTHENTICATION / REFRESH TOKEN INFRASTRUCTURE
        // ============================================================

        AddJwt(
            services,
            configuration);

        AddAuthenticationServices(
            services);

        // ============================================================
        // PHASE 1.6 + 1.11
        // CURRENT TENANT / CURRENT USER
        // ============================================================

        AddCurrentContexts(
            services);

        // ============================================================
        // PHASE 1.7
        // PERMISSION AUTHORIZATION
        // ============================================================

        AddPermissionAuthorization(
            services);

        // ============================================================
        // PHASE 1.12 - 1.16
        // TENANT APPLICATION SERVICES
        // ============================================================

        AddTenantServices(
            services);

        // ============================================================
        // PHASE 1.19
        // AUDIT LOGGING
        // ============================================================

        AddAuditing(
            services);
        
        AddSecurityLogging(services);

        AddPlatformServices(services, configuration);

        AddOrganizationServices(services);

        AddBillingAndAccountingServices(services);

        AddNotificationServices(services);

        AddVehicleServices(services);
            
        AddValidation(
            services);

        return services;
    }

    // ================================================================
    // DATABASE
    // PHASE 1.1 - 1.4
    // ================================================================

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<ConnectedOpsDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString);
            });
    }

    // ================================================================
    // IDENTITY
    // PHASE 1.2
    // ================================================================

    private static void AddIdentity(
        IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    // User
                    options.User.RequireUniqueEmail = true;

                    // Password
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ConnectedOpsDbContext>();
    }

    // ================================================================
    // JWT CONFIGURATION
    // PHASE 1.5
    // ================================================================

    private static void AddJwt(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection =
            configuration.GetSection("Jwt");

        var signingKey =
            jwtSection["SigningKey"];

        var issuer =
            jwtSection["Issuer"];

        var audience =
            jwtSection["Audience"];

        // ------------------------------------------------------------
        // Fail during startup instead of waiting for /api/auth/login.
        // ------------------------------------------------------------

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:SigningKey' is required.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:SigningKey' must be at least 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:Issuer' is required.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:Audience' is required.");
        }

        // ------------------------------------------------------------
        // Bind the SAME section already used by user-secrets:
        //
        // Jwt:SigningKey
        // Jwt:Issuer
        // Jwt:Audience
        // Jwt:AccessTokenMinutes
        // Jwt:RefreshTokenDays
        // ------------------------------------------------------------

        services
            .AddOptions<JwtSettings>()
            .Bind(jwtSection)
            .Validate(
                settings =>
                    !string.IsNullOrWhiteSpace(
                        settings.SigningKey),
                "Jwt:SigningKey is required.")
            .Validate(
                settings =>
                    Encoding.UTF8.GetByteCount(
                        settings.SigningKey) >= 32,
                "Jwt:SigningKey must be at least 32 bytes.")
            .Validate(
                settings =>
                    !string.IsNullOrWhiteSpace(
                        settings.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                settings =>
                    !string.IsNullOrWhiteSpace(
                        settings.Audience),
                "Jwt:Audience is required.")
            .Validate(
                settings =>
                    settings.AccessTokenMinutes > 0,
                "Jwt:AccessTokenMinutes must be greater than zero.")
            .Validate(
                settings =>
                    settings.RefreshTokenDays > 0,
                "Jwt:RefreshTokenDays must be greater than zero.")
            .ValidateOnStart();
    }

    // ================================================================
    // AUTHENTICATION
    // PHASE 1.5 / 1.8 / 1.9 / 1.10 / 1.12
    // ================================================================

    private static void AddAuthenticationServices(
        IServiceCollection services)
    {
        // Main login / tenant selection / refresh / logout service
        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        // Access JWT + tenant-selection JWT
        services.AddScoped<
            IJwtTokenService,
            JwtTokenService>();

        // Invitation token generator / hasher
        services.AddScoped<
            IInvitationTokenService,
            InvitationTokenService>();

        //refresh token generator / hasher
        services.AddScoped<
             IRefreshTokenService,
              RefreshTokenService>();
    }

    // ================================================================
    // CURRENT REQUEST CONTEXT
    // PHASE 1.6 + 1.11
    // ================================================================

    private static void AddCurrentContexts(
        IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // ------------------------------------------------------------
        // PHASE 1.6
        // Current tenant from authenticated tenant_id claim.
        // ------------------------------------------------------------

        services.AddScoped<
            ICurrentTenantContext,
            CurrentTenantContext>();

        // ------------------------------------------------------------
        // PHASE 1.11
        // Current authenticated user / tenant claims.
        // ------------------------------------------------------------

        services.AddScoped<
            ICurrentUserContext,
            CurrentUserContext>();

        // ------------------------------------------------------------
        // PHASE 1.11
        // /api/auth/me service
        // ------------------------------------------------------------

        services.AddScoped<
            ICurrentUserService,
            CurrentUserService>();
    }

    // ================================================================
    // PERMISSION AUTHORIZATION
    // PHASE 1.7
    // ================================================================

    private static void AddPermissionAuthorization(
        IServiceCollection services)
    {
        // ------------------------------------------------------------
        // Dynamic policies:
        //
        // Permission:Users.View
        // Permission:Users.Create
        // Permission:Vehicles.View
        // etc.
        // ------------------------------------------------------------

        services.AddSingleton<
            IAuthorizationPolicyProvider,
            PermissionPolicyProvider>();

        // ------------------------------------------------------------
        // Evaluates TenantUser -> Role -> Permission relationships.
        //
        // Scoped because the handler accesses tenant/database state.
        // ------------------------------------------------------------

        services.AddScoped<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            PlatformRoleAuthorizationHandler>();
    }

    // ================================================================
    // TENANT SERVICES
    // PHASE 1.12 - 1.16
    // ================================================================

    private static void AddTenantServices(
        IServiceCollection services)
    {
        // ------------------------------------------------------------
        // PHASE 1.12
        // Invitations
        // ------------------------------------------------------------

        services.AddScoped<
            IInvitationService,
            InvitationService>();

        // ------------------------------------------------------------
        // PHASE 1.13
        // Tenant User Management
        // ------------------------------------------------------------

        services.AddScoped<
            ITenantUserManagementService,
            TenantUserManagementService>();

        // ------------------------------------------------------------
        // PHASE 1.14
        // Tenant Role Management
        // ------------------------------------------------------------

        services.AddScoped<
            ITenantRoleManagementService,
            TenantRoleManagementService>();

        // ------------------------------------------------------------
        // PHASE 1.15
        // Permission Management
        // ------------------------------------------------------------

        services.AddScoped<
            IPermissionManagementService,
            PermissionManagementService>();

        // ------------------------------------------------------------
        // PHASE 1.16
        // Tenant Settings
        // ------------------------------------------------------------

        services.AddScoped<
            ITenantSettingsService,
            ConnectedOps.Infrastructure
                .TenantSettings
                .TenantSettingsService>();
    }

    // ================================================================
    // VALIDATION
    // PHASE 1.18
    // ================================================================

    private static void AddValidation(
        IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<
            ApplicationAssembly>(
            ServiceLifetime.Scoped);
    }

    // ================================================================
    //PHASE 1.19 AUDITING
    // ================================================================
    private static void AddAuditing(
    IServiceCollection services)
    {
        services.AddScoped<
            IAuditLogService,
            AuditLogService>();
    }
    // ================================================================


    // ================================================================
    //PHASE 1.20 Secuirity Logging
    // ================================================================
    private static void AddSecurityLogging(
    IServiceCollection services)
    {
        services.AddScoped<
            ISecurityLogService,
            SecurityLogService>();
    }

    // ================================================================
    // PHASE 1.22 PLATFORM SERVICES
    // ================================================================
    private static void AddPlatformServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PlatformBootstrapOptions>(
            configuration.GetSection(PlatformBootstrapOptions.SectionName));

        services.AddScoped<
            IPlatformTenantService,
            PlatformTenantService>();

        services.AddScoped<
            IPlatformUserService,
            PlatformUserService>();

        services.AddScoped<
            IPlatformDashboardService,
            PlatformDashboardService>();

        services.AddScoped<
            IPlatformAuditService,
            PlatformAuditService>();

        services.AddScoped<
            IPlatformSecurityService,
            PlatformSecurityService>();

        services.AddScoped<
            IPlatformSettingsService,
            PlatformSettingsService>();
    }

    // ================================================================
    // PHASE 2 - ORGANIZATION & USER MANAGEMENT
    // ================================================================
    private static void AddOrganizationServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IOrganizationProfileService,
            OrganizationProfileService>();

        services.AddScoped<
            IBranchService,
            BranchService>();

        services.AddScoped<
            ILocationService,
            LocationService>();

        services.AddScoped<
            IDepartmentService,
            DepartmentService>();

        services.AddScoped<
            ITeamService,
            TeamService>();

        services.AddScoped<
            IEmployeeService,
            EmployeeService>();

        services.AddScoped<
            IOrganizationHierarchyService,
            OrganizationHierarchyService>();

        services.AddScoped<
            IOrganizationDashboardService,
            OrganizationDashboardService>();

        services.AddScoped<
            IOrganizationSettingsService,
            OrganizationSettingsService>();

        services.AddScoped<
            ITenantUserAdministrationService,
            TenantUserAdministrationService>();
    }

    // ================================================================
    // PHASE 3 - BILLING, SUBSCRIPTIONS & ACCOUNTING
    // ================================================================
    private static void AddBillingAndAccountingServices(
        IServiceCollection services)
    {
        services.AddScoped<
            ISubscriptionPlanService,
            SubscriptionPlanService>();

        services.AddScoped<
            ITenantSubscriptionService,
            TenantSubscriptionService>();

        services.AddScoped<
            IInvoiceService,
            InvoiceService>();

        services.AddScoped<
            IPaymentService,
            PaymentService>();

        services.AddScoped<
            IBillingDashboardService,
            BillingDashboardService>();

        services.AddScoped<
            IAccountingLedgerService,
            AccountingLedgerService>();
    }

    // ================================================================
    // PHASE 3 - NOTIFICATIONS
    // ================================================================
    private static void AddNotificationServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IEmailNotificationService,
            EmailNotificationService>();
    }

    // ================================================================
    // PHASE 3 - VEHICLE MANAGEMENT
    // ================================================================
    private static void AddVehicleServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IVehicleCategoryService,
            VehicleCategoryService>();

        services.AddScoped<
            IVehicleMakeModelService,
            VehicleMakeModelService>();

        services.AddScoped<
            IVehicleOdometerService,
            VehicleOdometerService>();

        services.AddScoped<
            IVehicleDocumentService,
            VehicleDocumentService>();

        services.AddScoped<
            IVehicleDashboardService,
            VehicleDashboardService>();

        services.AddScoped<
            IVehicleService,
            VehicleService>();
    }
}
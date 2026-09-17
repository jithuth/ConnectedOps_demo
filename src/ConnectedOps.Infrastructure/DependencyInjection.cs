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
using ConnectedOps.Application.Drivers;
using ConnectedOps.Infrastructure.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Infrastructure.FleetOperations;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Infrastructure.Telematics;
using ConnectedOps.Infrastructure.Telematics.Providers;
using ConnectedOps.Application.Maps;
using ConnectedOps.Infrastructure.Maps;
using ConnectedOps.Application.Geofences;
using ConnectedOps.Infrastructure.Geofences;
using ConnectedOps.Application.Demo;
using ConnectedOps.Infrastructure.Demo;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Infrastructure.Maintenance;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Infrastructure.Fuel;
using ConnectedOps.Application.Assets;
using ConnectedOps.Infrastructure.Assets;
using ConnectedOps.Application.Compliance;
using ConnectedOps.Infrastructure.Compliance;
using ConnectedOps.Application.Safety;
using ConnectedOps.Infrastructure.Safety;
using ConnectedOps.Application.Reports;
using ConnectedOps.Infrastructure.Reports;
using ConnectedOps.Application.Dispatch;
using ConnectedOps.Infrastructure.Dispatch;
using ConnectedOps.Application.Alerts;
using ConnectedOps.Infrastructure.Alerts;
using ConnectedOps.Application.Inspections;
using ConnectedOps.Infrastructure.Inspections;
using ConnectedOps.Application.TollsAndFines;
using ConnectedOps.Infrastructure.TollsAndFines;
using ConnectedOps.Application.ColdChain;
using ConnectedOps.Infrastructure.ColdChain;
using ConnectedOps.Application.Hos;
using ConnectedOps.Infrastructure.Hos;
using ConnectedOps.Application.Gamification;
using ConnectedOps.Infrastructure.Gamification;
using ConnectedOps.Application.Expenses;
using ConnectedOps.Infrastructure.Expenses;
using ConnectedOps.Application.Tracking;
using ConnectedOps.Infrastructure.Tracking;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Infrastructure.Integrations;
using ConnectedOps.Application.Predictive;
using ConnectedOps.Infrastructure.Predictive;
using ConnectedOps.Application.Ev;
using ConnectedOps.Infrastructure.Ev;
using ConnectedOps.Application.Optimization;
using ConnectedOps.Infrastructure.Optimization;
using ConnectedOps.Application.WhiteLabel;
using ConnectedOps.Infrastructure.WhiteLabel;



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

        AddDriverServices(services);
            
        AddFleetOperationsServices(services);

        AddTelematicsServices(services);

        AddMapsAndGeofencingServices(services, configuration);

        AddMaintenanceServices(services);

        AddFuelServices(services);

        AddAssetServices(services);

        AddComplianceAndSafetyServices(services);

        AddReportServices(services);

        AddDispatchServices(services);

        AddOperationalIntelligenceServices(services);

        AddDriverExperienceServices(services);

        AddIntegrationServices(services);

        AddPredictiveMaintenanceServices(services);

        AddEvServices(services);

        AddOptimizationServices(services);

        AddWhiteLabelServices(services);

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
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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

    // ================================================================
    // PHASE 4 - DRIVER MANAGEMENT
    // ================================================================
    private static void AddDriverServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IDriverEligibilityService,
            DriverEligibilityService>();

        services.AddScoped<
            IDriverAssignmentService,
            DriverAssignmentService>();

        services.AddScoped<
            IDriverLicenseService,
            DriverLicenseService>();

        services.AddScoped<
            IDriverCertificationService,
            DriverCertificationService>();

        services.AddScoped<
            IDriverDocumentService,
            DriverDocumentService>();

        services.AddScoped<
            IDriverDashboardService,
            DriverDashboardService>();

        services.AddScoped<
            IDriverService,
            DriverService>();
    }

    // ================================================================
    // PHASE 5 - FLEET OPERATIONS & DAILY OPERATIONS
    // ================================================================
    private static void AddFleetOperationsServices(
        IServiceCollection services)
    {
        services.AddScoped<
            IFleetAvailabilityService,
            FleetAvailabilityService>();

        services.AddScoped<
            IFleetShiftService,
            FleetShiftService>();

        services.AddScoped<
            IFleetShiftAssignmentService,
            FleetShiftAssignmentService>();

        services.AddScoped<
            IVehicleUsageSessionService,
            VehicleUsageSessionService>();

        services.AddScoped<
            IVehicleHandoverService,
            VehicleHandoverService>();

        services.AddScoped<
            IFleetOperationalExceptionService,
            FleetOperationalExceptionService>();

        services.AddScoped<
            IFleetActivityTimelineService,
            FleetActivityTimelineService>();

        services.AddScoped<
            IFleetOperationsDashboardService,
            FleetOperationsDashboardService>();
    }

    // ================================================================
    // PHASE 6 - GPS, TRACKING DEVICES & TELEMATICS
    // ================================================================
    private static void AddTelematicsServices(
        IServiceCollection services)
    {
        services.AddScoped<ITelematicsProvider, TeltonikaTelematicsProvider>();
        services.AddScoped<IDeviceConnectivityService, DeviceConnectivityService>();
        services.AddScoped<ITelemetryValidationService, TelemetryValidationService>();
        services.AddScoped<ITelemetryDeduplicationService, TelemetryDeduplicationService>();
        services.AddScoped<ITrackingProviderService, TrackingProviderService>();
        services.AddScoped<ITrackingDeviceService, TrackingDeviceService>();
        services.AddScoped<IDeviceProvisioningService, DeviceProvisioningService>();
        services.AddScoped<IDeviceAssignmentService, DeviceAssignmentService>();
        services.AddScoped<ITelemetryIngestionService, TelemetryIngestionService>();
        services.AddScoped<IVehicleTelemetryStateService, VehicleTelemetryStateService>();
        services.AddScoped<ITelemetryHistoryService, TelemetryHistoryService>();
        services.AddScoped<IDeviceHealthService, DeviceHealthService>();
        services.AddScoped<IDeviceCommandService, DeviceCommandService>();
        services.AddScoped<ITelematicsDashboardService, TelematicsDashboardService>();
        services.AddScoped<ITelematicsSettingsService, TelematicsSettingsService>();
    }

    // ================================================================
    // PHASE 7 - MAPS, GEOFENCING & DEMO VEHICLE SIMULATION
    // ================================================================
    private static void AddMapsAndGeofencingServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MapSettings>(configuration.GetSection(MapSettings.SectionName));
        services.Configure<DemoFleetSettings>(configuration.GetSection(DemoFleetSettings.SectionName));

        services.AddScoped<IFleetMapService, FleetMapService>();
        services.AddScoped<IGeofenceService, GeofenceService>();
        services.AddScoped<IGeofenceEvaluationService, GeofenceEvaluationService>();
        services.AddScoped<IDemoFleetSimulator, DemoFleetSimulator>();
        services.AddScoped<IMasterEnterpriseDemoSeeder, MasterEnterpriseDemoSeeder>();
        services.AddHostedService<DemoFleetBackgroundService>();
    }

    // ================================================================
    // PHASE 8 - VEHICLE MAINTENANCE MANAGEMENT
    // ================================================================
    private static void AddMaintenanceServices(
        IServiceCollection services)
    {
        services.AddScoped<IMaintenanceServiceTypeService, MaintenanceServiceTypeService>();
        services.AddScoped<IMaintenanceProviderService, MaintenanceProviderService>();
        services.AddScoped<IMaintenancePlanService, MaintenancePlanService>();
        services.AddScoped<IVehicleEngineHoursProvider, VehicleEngineHoursProvider>();
        services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();
        services.AddScoped<IMaintenanceDueEvaluationService, MaintenanceDueEvaluationService>();
        services.AddScoped<IMaintenanceRecordService, MaintenanceRecordService>();
        services.AddScoped<IMaintenanceDashboardService, MaintenanceDashboardService>();
    }

    // ================================================================
    // PHASE 9 - FUEL MANAGEMENT
    // ================================================================
    private static void AddFuelServices(
        IServiceCollection services)
    {
        services.AddScoped<IFuelTypeService, FuelTypeService>();
        services.AddScoped<IFuelStationService, FuelStationService>();
        services.AddScoped<IFuelCardService, FuelCardService>();
        services.AddScoped<IFuelEfficiencyService, FuelEfficiencyService>();
        services.AddScoped<IFuelAnomalyService, FuelAnomalyService>();
        services.AddScoped<IFuelTransactionService, FuelTransactionService>();
        services.AddScoped<IFuelAnalyticsService, FuelAnalyticsService>();
        services.AddScoped<IFuelDashboardService, FuelDashboardService>();
        services.AddScoped<ITelematicsFuelProvider, TelematicsFuelProvider>();
        services.AddScoped<IFuelImportService, FuelImportService>();
    }

    // ================================================================
    // PHASE 10 - ASSET & EQUIPMENT MANAGEMENT
    // ================================================================
    private static void AddAssetServices(
        IServiceCollection services)
    {
        services.AddScoped<IAssetQrCodeService, AssetQrCodeService>();
        services.AddScoped<IAssetCategoryService, AssetCategoryService>();
        services.AddScoped<IAssetTypeService, AssetTypeService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetCustodyService, AssetCustodyService>();
        services.AddScoped<IAssetTransferService, AssetTransferService>();
        services.AddScoped<IAssetInspectionService, AssetInspectionService>();
        services.AddScoped<IAssetConditionService, AssetConditionService>();
        services.AddScoped<IAssetDocumentService, AssetDocumentService>();
        services.AddScoped<IAssetIdentifierService, AssetIdentifierService>();
        services.AddScoped<IAssetUtilizationService, AssetUtilizationService>();
        services.AddScoped<IAssetActivityTimelineService, AssetActivityTimelineService>();
        services.AddScoped<IAssetDashboardService, AssetDashboardService>();
    }

    // ================================================================
    // PHASE 11 - COMPLIANCE & SAFETY MANAGEMENT
    // ================================================================
    private static void AddComplianceAndSafetyServices(
        IServiceCollection services)
    {
        services.AddScoped<IComplianceExpiryService, ComplianceExpiryService>();
        services.AddScoped<IComplianceStatusService, ComplianceStatusService>();
        services.AddScoped<IComplianceScoreService, ComplianceScoreService>();
        services.AddScoped<IComplianceRequirementService, ComplianceRequirementService>();
        services.AddScoped<IComplianceRecordService, ComplianceRecordService>();
        services.AddScoped<IComplianceExceptionService, ComplianceExceptionService>();
        services.AddScoped<IComplianceEvaluationService, ComplianceEvaluationService>();
        services.AddScoped<IComplianceDashboardService, ComplianceDashboardService>();

        services.AddScoped<IIncidentNumberGenerator, IncidentNumberGenerator>();
        services.AddScoped<ISafetyIncidentService, SafetyIncidentService>();
        services.AddScoped<ISafetyViolationService, SafetyViolationService>();
        services.AddScoped<ICorrectiveActionService, CorrectiveActionService>();
        services.AddScoped<ISafetyScoreService, SafetyScoreService>();
        services.AddScoped<ISafetyDashboardService, SafetyDashboardService>();
    }

    // ================================================================
    // PHASE 12 - ADVANCED BI, EXECUTIVE REPORTS & ESG
    // ================================================================
    private static void AddReportServices(
        IServiceCollection services)
    {
        services.AddScoped<IExecutiveReportService, ExecutiveReportService>();
    }

    // ================================================================
    // PHASE 13 - ROUTE PLANNING, DISPATCH & JOB MANAGEMENT
    // ================================================================
    private static void AddDispatchServices(
        IServiceCollection services)
    {
        services.AddScoped<IDispatchService, DispatchService>();
    }

    // ================================================================
    // PHASE 14 - OPERATIONAL INTELLIGENCE SUITE
    // ================================================================
    private static void AddOperationalIntelligenceServices(
        IServiceCollection services)
    {
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IDvirService, DvirService>();
        services.AddScoped<ITollAndFineService, TollAndFineService>();
        services.AddScoped<IColdChainService, ColdChainService>();
    }

    // ================================================================
    // PHASE 15 - DRIVER IN-CAB EXPERIENCE, HOS, GAMIFICATION & TRACKING
    // ================================================================
    private static void AddDriverExperienceServices(
        IServiceCollection services)
    {
        services.AddScoped<IHosService, HosService>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<IDriverExpenseService, DriverExpenseService>();
        services.AddScoped<IPublicTrackingService, PublicTrackingService>();
    }

    // ================================================================
    // PHASE 16 - EXTERNAL INTEGRATIONS, WEBHOOKS & OPEN API GATEWAY
    // ================================================================
    private static void AddIntegrationServices(
        IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IErpExportService, ErpExportService>();
        services.AddScoped<IFuelClearinghouseService, FuelClearinghouseService>();
    }

    // ================================================================
    // PHASE 17 - AI PREDICTIVE FLEET MAINTENANCE & SUBSYSTEM HEALTH
    // ================================================================
    private static void AddPredictiveMaintenanceServices(
        IServiceCollection services)
    {
        services.AddScoped<IPredictiveMaintenanceService, PredictiveMaintenanceService>();
    }

    // ================================================================
    // PHASE 18 - EV FLEET MANAGEMENT & BATTERY TELEMETRY
    // ================================================================
    private static void AddEvServices(
        IServiceCollection services)
    {
        services.AddScoped<IEvService, EvService>();
    }

    // ================================================================
    // PHASE 19 - AI ROUTE OPTIMIZATION & VRP SOLVER
    // ================================================================
    private static void AddOptimizationServices(
        IServiceCollection services)
    {
        services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
    }

    // ================================================================
    // PHASE 20 - ENTERPRISE WHITE-LABELING & AUDIT COMPLIANCE
    // ================================================================
    private static void AddWhiteLabelServices(
        IServiceCollection services)
    {
        services.AddScoped<IWhiteLabelService, WhiteLabelService>();
    }
}
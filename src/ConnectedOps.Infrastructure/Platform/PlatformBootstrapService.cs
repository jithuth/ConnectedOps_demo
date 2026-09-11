using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Domain.Accounting;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Platform;

public static class PlatformBootstrapService
{
    public static async Task BootstrapPlatformAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(PlatformBootstrapService));

        var options = scope.ServiceProvider.GetRequiredService<IOptions<PlatformBootstrapOptions>>().Value;

        if (!options.Enabled)
        {
            logger.LogInformation("Platform bootstrap is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Platform bootstrap is enabled but Email or Password is not provided.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = options.Email.Trim();

        var existingUser = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(email);

        ApplicationUser superAdmin;

        if (existingUser is null)
        {
            logger.LogInformation("Creating initial SuperAdmin user for {Email}.", email);

            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = string.IsNullOrWhiteSpace(options.FirstName) ? "Platform" : options.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(options.LastName) ? "SuperAdmin" : options.LastName.Trim(),
                PlatformRole = PlatformRole.SuperAdmin,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                var result = await userManager.CreateAsync(superAdmin, options.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create SuperAdmin user: {Errors}", errors);
                    throw new InvalidOperationException($"Platform bootstrap failed: {errors}");
                }

                logger.LogInformation("SuperAdmin user created successfully.");
            }
            catch (DbUpdateException)
            {
                // Handle concurrent bootstrap execution across multiple processes/nodes
                var concurrentUser = await userManager.FindByEmailAsync(email)
                    ?? await userManager.FindByNameAsync(email);

                if (concurrentUser is not null && concurrentUser.PlatformRole == PlatformRole.SuperAdmin)
                {
                    logger.LogInformation("SuperAdmin user {Email} already exists (concurrent bootstrap). Platform bootstrap skipped (idempotent).", email);
                    superAdmin = concurrentUser;
                }
                else
                {
                    throw;
                }
            }
        }
        else if (existingUser.PlatformRole == PlatformRole.SuperAdmin)
        {
            logger.LogInformation("SuperAdmin user {Email} already exists.", email);
            superAdmin = existingUser;
        }
        else
        {
            logger.LogError("Email {Email} is already registered as a non-SuperAdmin user. Cannot promote to SuperAdmin via bootstrap.", email);
            throw new InvalidOperationException($"Platform bootstrap failed: User {email} exists and is not a SuperAdmin. Promotion is rejected.");
        }

        // Ensure default tenant exists and SuperAdmin is linked
        var dbContext = scope.ServiceProvider.GetService<ConnectedOpsDbContext>();
        if (dbContext is not null)
        {
            await EnsureDefaultSubscriptionPlansAsync(dbContext, logger);

            var hasAnyTenant = await dbContext.Tenants.IgnoreQueryFilters().AnyAsync();
            if (!hasAnyTenant)
            {
                await ProvisionDefaultTenantAsync(dbContext, superAdmin, logger);
            }
            else
            {
                await EnsureSuperAdminTenantMembershipAsync(dbContext, superAdmin, logger);
            }

            var defaultTenant = await dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Code == "PRIMARY-OPS");
            if (defaultTenant is not null)
            {
                await EnsureTenantBillingAndAccountingAsync(dbContext, defaultTenant.Id, logger);
            }
        }
    }

    private static async Task ProvisionDefaultTenantAsync(
        ConnectedOpsDbContext dbContext,
        ApplicationUser superAdmin,
        ILogger logger)
    {
        try
        {
            var existingTenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == "PRIMARY-OPS");

            if (existingTenant is not null)
            {
                await EnsureSuperAdminTenantMembershipAsync(dbContext, superAdmin, logger);
                return;
            }

            logger.LogInformation("Provisioning default initial tenant 'Primary Operations Fleet'...");

            var tenant = new Tenant(
                "Primary Operations Fleet",
                "PRIMARY-OPS",
                superAdmin.Email);

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();

            var roles = CreateDefaultRoles(tenant.Id);
            dbContext.TenantRoles.AddRange(roles);
            await dbContext.SaveChangesAsync();

            var permissions = await dbContext.Permissions
                .Where(x => x.IsActive)
                .ToListAsync();

            var ownerRole = roles.Single(x => x.Code == TenantRoleCodes.TenantOwner);

            foreach (var permission in permissions)
            {
                dbContext.RolePermissions.Add(
                    new RolePermission(ownerRole.Id, permission.Id));
            }

            var tenantUser = new TenantUser(
                tenant.Id,
                superAdmin.Id,
                isDefaultTenant: true);

            dbContext.TenantUsers.Add(tenantUser);
            await dbContext.SaveChangesAsync();

            var tenantUserRole = new TenantUserRole(
                tenantUser.Id,
                ownerRole.Id);

            dbContext.TenantUserRoles.Add(tenantUserRole);

            // Provision initial Organization Profile & Hierarchy
            var profile = new OrganizationProfile(
                tenant.Id,
                "Primary Operations Fleet",
                "PRIMARY-OPS",
                null,
                null,
                null,
                superAdmin.Email,
                null,
                "100 Enterprise Way",
                null,
                "Chicago",
                "IL",
                "60601",
                "USA",
                "USD",
                "UTC");

            dbContext.OrganizationProfiles.Add(profile);

            var hqBranch = new Branch(
                tenant.Id,
                "Central Headquarters",
                "HQ-01",
                BranchType.HeadOffice,
                isHeadOffice: true,
                email: superAdmin.Email,
                city: "Chicago",
                stateOrProvince: "IL",
                countryCode: "USA");

            dbContext.Branches.Add(hqBranch);
            await dbContext.SaveChangesAsync();

            var mainLocation = new Location(
                tenant.Id,
                hqBranch.Id,
                "Primary Fleet Depot & Yard",
                "DEPOT-01",
                LocationType.Yard,
                addressLine1: "100 Enterprise Way",
                city: "Chicago",
                stateOrProvince: "IL",
                countryCode: "USA");

            dbContext.Locations.Add(mainLocation);

            var opsDept = new Department(
                tenant.Id,
                "Fleet Operations",
                "OPS",
                hqBranch.Id,
                description: "Core fleet logistics, vehicle management, and asset dispatch");

            dbContext.Departments.Add(opsDept);
            await dbContext.SaveChangesAsync();

            var dispatchTeam = new Team(
                tenant.Id,
                opsDept.Id,
                "Dispatch & Fleet Control",
                "DISPATCH-01",
                description: "Real-time dispatch, route planning, and operator communication");

            dbContext.Teams.Add(dispatchTeam);

            var orgSettings = new OrganizationSettings(tenant.Id);
            dbContext.OrganizationSettings.Add(orgSettings);

            await dbContext.SaveChangesAsync();

            await EnsureTenantBillingAndAccountingAsync(dbContext, tenant.Id, logger);

            logger.LogInformation("Default initial tenant 'Primary Operations Fleet' (Code: PRIMARY-OPS) provisioned successfully.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogInformation("Tenant provisioning handled concurrently by another process: {Message}", ex.Message);
        }
    }

    private static async Task EnsureSuperAdminTenantMembershipAsync(
        ConnectedOpsDbContext dbContext,
        ApplicationUser superAdmin,
        ILogger logger)
    {
        var defaultTenant = await dbContext.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .OrderBy(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (defaultTenant is null)
        {
            return;
        }

        var existingMembership = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(tu => tu.TenantId == defaultTenant.Id && tu.UserId == superAdmin.Id);

        var ownerRole = await dbContext.TenantRoles
            .FirstOrDefaultAsync(r => r.TenantId == defaultTenant.Id && r.Code == TenantRoleCodes.TenantOwner);

        if (existingMembership is not null)
        {
            if (!existingMembership.IsActive)
            {
                existingMembership.Activate();
                await dbContext.SaveChangesAsync();
            }

            if (ownerRole is not null)
            {
                var hasRole = await dbContext.TenantUserRoles
                    .AnyAsync(tur => tur.TenantUserId == existingMembership.Id && tur.TenantRoleId == ownerRole.Id);

                if (!hasRole)
                {
                    try
                    {
                        dbContext.TenantUserRoles.Add(new TenantUserRole(existingMembership.Id, ownerRole.Id));
                        await dbContext.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Concurrent role assignment handled
                    }
                }
            }

            logger.LogInformation("SuperAdmin {Email} already linked to tenant {TenantName} ({TenantCode}).", superAdmin.Email, defaultTenant.Name, defaultTenant.Code);
            return;
        }

        try
        {
            var tenantUser = new TenantUser(defaultTenant.Id, superAdmin.Id, isDefaultTenant: true);
            dbContext.TenantUsers.Add(tenantUser);
            await dbContext.SaveChangesAsync();

            if (ownerRole is not null)
            {
                dbContext.TenantUserRoles.Add(new TenantUserRole(tenantUser.Id, ownerRole.Id));
                await dbContext.SaveChangesAsync();
            }

            logger.LogInformation("Linked SuperAdmin {Email} to existing tenant {TenantName} ({TenantCode}).", superAdmin.Email, defaultTenant.Name, defaultTenant.Code);
        }
        catch (DbUpdateException)
        {
            logger.LogInformation("SuperAdmin {Email} tenant membership created concurrently.", superAdmin.Email);
        }
    }

    private static List<TenantRole> CreateDefaultRoles(Guid tenantId)
    {
        return
        [
            new TenantRole(tenantId, "Tenant Owner", TenantRoleCodes.TenantOwner, "Full administrative control of the tenant.", true),
            new TenantRole(tenantId, "Company Admin", TenantRoleCodes.CompanyAdmin, "Manages company users and configuration.", true),
            new TenantRole(tenantId, "Fleet Manager", TenantRoleCodes.FleetManager, "Manages fleet vehicles and drivers.", true),
            new TenantRole(tenantId, "Dispatcher", TenantRoleCodes.Dispatcher, "Handles vehicle and driver operations.", true),
            new TenantRole(tenantId, "Maintenance Manager", TenantRoleCodes.MaintenanceManager, "Manages fleet maintenance operations.", true),
            new TenantRole(tenantId, "Safety Officer", TenantRoleCodes.SafetyOfficer, "Manages fleet safety activities.", true),
            new TenantRole(tenantId, "Asset Manager", TenantRoleCodes.AssetManager, "Manages company assets and equipment.", true),
            new TenantRole(tenantId, "Driver", TenantRoleCodes.Driver, "Fleet driver access.", true)
        ];
    }

    private static async Task EnsureDefaultSubscriptionPlansAsync(
        ConnectedOpsDbContext dbContext,
        ILogger logger)
    {
        var hasPlans = await dbContext.SubscriptionPlans.IgnoreQueryFilters().AnyAsync();
        if (hasPlans)
        {
            return;
        }

        logger.LogInformation("Seeding default subscription plans (Starter, Professional, Enterprise)...");

        var starter = new SubscriptionPlan(
            "Starter Fleet",
            "STARTER",
            49.00m,
            BillingInterval.Monthly,
            "USD",
            "Essential fleet and asset intelligence for small operations",
            maxVehicles: 10,
            maxAssets: 25,
            maxUsers: 3,
            maxStorageGb: 5,
            hasAdvancedAnalytics: false,
            hasApiAccess: false,
            hasCustomBranding: false,
            hasAuditExport: false,
            isPublic: true,
            sortOrder: 1);

        var pro = new SubscriptionPlan(
            "Professional Fleet",
            "PRO",
            149.00m,
            BillingInterval.Monthly,
            "USD",
            "Complete fleet telematics, asset tracking, and team management",
            maxVehicles: 50,
            maxAssets: 150,
            maxUsers: 15,
            maxStorageGb: 25,
            hasAdvancedAnalytics: true,
            hasApiAccess: true,
            hasCustomBranding: false,
            hasAuditExport: true,
            isPublic: true,
            sortOrder: 2);

        var enterprise = new SubscriptionPlan(
            "Enterprise Scale",
            "ENTERPRISE",
            499.00m,
            BillingInterval.Monthly,
            "USD",
            "Unlimited fleet intelligence, multi-branch hierarchy, and custom integrations",
            maxVehicles: 500,
            maxAssets: 2000,
            maxUsers: 100,
            maxStorageGb: 200,
            hasAdvancedAnalytics: true,
            hasApiAccess: true,
            hasCustomBranding: true,
            hasAuditExport: true,
            isPublic: true,
            sortOrder: 3);

        dbContext.SubscriptionPlans.AddRange(starter, pro, enterprise);
        try
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Default subscription plans seeded successfully.");
        }
        catch (DbUpdateException)
        {
            // Handled concurrently
        }
    }

    private static async Task EnsureTenantBillingAndAccountingAsync(
        ConnectedOpsDbContext dbContext,
        Guid tenantId,
        ILogger logger)
    {
        // 1. Ensure Subscription
        var hasSubscription = await dbContext.TenantSubscriptions
            .IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == tenantId);

        if (!hasSubscription)
        {
            var proPlan = await dbContext.SubscriptionPlans
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Code == "PRO")
                ?? await dbContext.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync();

            if (proPlan is not null)
            {
                var subscription = new TenantSubscription(
                    tenantId,
                    proPlan.Id,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddMonths(1),
                    SubscriptionStatus.Active,
                    autoRenew: true);

                dbContext.TenantSubscriptions.Add(subscription);
            }
        }

        // 2. Ensure Default Chart of Accounts
        var hasAccounts = await dbContext.GeneralLedgerAccounts
            .IgnoreQueryFilters()
            .AnyAsync(a => a.TenantId == tenantId);

        if (!hasAccounts)
        {
            var defaultAccounts = new List<GeneralLedgerAccount>
            {
                new(tenantId, "1000", "Operating Cash Account", AccountCategory.Asset, "Primary operational checking and cash account", isSystem: true),
                new(tenantId, "1200", "Accounts Receivable", AccountCategory.Asset, "Outstanding customer and tenant invoices", isSystem: true),
                new(tenantId, "2000", "Accounts Payable", AccountCategory.Liability, "Vendor and platform liabilities", isSystem: true),
                new(tenantId, "3000", "Retained Earnings & Equity", AccountCategory.Equity, "Cumulative net surplus / equity", isSystem: true),
                new(tenantId, "4000", "SaaS Subscription Revenue", AccountCategory.Revenue, "Core SaaS subscription recurring billing revenue", isSystem: true),
                new(tenantId, "4100", "Telematics & Add-on Revenue", AccountCategory.Revenue, "Add-on device, sensor, and storage revenue", isSystem: true),
                new(tenantId, "5000", "Fleet Operating Expense", AccountCategory.Expense, "Fleet maintenance and direct operational costs", isSystem: true),
                new(tenantId, "5100", "IoT & Telematics Service Expense", AccountCategory.Expense, "Cellular connectivity and IoT cloud fees", isSystem: true),
                new(tenantId, "5200", "General & Administrative Expense", AccountCategory.Expense, "Overhead, utilities, and general administration", isSystem: true)
            };

            dbContext.GeneralLedgerAccounts.AddRange(defaultAccounts);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Handled concurrently
        }
    }
}

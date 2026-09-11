using ConnectedOps.Domain.Authorization;
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
            var hasAnyTenant = await dbContext.Tenants.IgnoreQueryFilters().AnyAsync();
            if (!hasAnyTenant)
            {
                await ProvisionDefaultTenantAsync(dbContext, superAdmin, logger);
            }
            else
            {
                await EnsureSuperAdminTenantMembershipAsync(dbContext, superAdmin, logger);
            }
        }
    }

    private static async Task ProvisionDefaultTenantAsync(
        ConnectedOpsDbContext dbContext,
        ApplicationUser superAdmin,
        ILogger logger)
    {
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

        logger.LogInformation("Default initial tenant 'Primary Operations Fleet' (Code: PRIMARY-OPS) provisioned successfully.");
    }

    private static async Task EnsureSuperAdminTenantMembershipAsync(
        ConnectedOpsDbContext dbContext,
        ApplicationUser superAdmin,
        ILogger logger)
    {
        var hasMembership = await dbContext.TenantUsers.AnyAsync(tu => tu.UserId == superAdmin.Id && tu.IsActive);
        if (!hasMembership)
        {
            var defaultTenant = await dbContext.Tenants
                .Where(t => t.Status == TenantStatus.Active)
                .OrderBy(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (defaultTenant is not null)
            {
                var ownerRole = await dbContext.TenantRoles
                    .FirstOrDefaultAsync(r => r.TenantId == defaultTenant.Id && r.Code == TenantRoleCodes.TenantOwner);

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
}

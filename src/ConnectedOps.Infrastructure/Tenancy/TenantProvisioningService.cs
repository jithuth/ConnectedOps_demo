using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Tenants;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Tenancy;

public sealed class TenantProvisioningService
    : ITenantProvisioningService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public TenantProvisioningService(
        ConnectedOpsDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<CreateTenantResult> CreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var normalizedCode =
            request.CompanyCode.Trim().ToUpperInvariant();

        var normalizedEmail =
            request.OwnerEmail.Trim().ToLowerInvariant();

        var tenantExists =
            await _dbContext.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(
                    x => x.Code == normalizedCode,
                    cancellationToken);

        if (tenantExists)
        {
            throw new ConflictException(
                $"Tenant code '{normalizedCode}' already exists.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(
                normalizedEmail);

        if (existingUser is not null)
        {
            throw new ConflictException(
                $"A user with email '{normalizedEmail}' already exists.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            //
            // 1. Create Tenant
            //

            var tenant = new Tenant(
                request.CompanyName,
                normalizedCode,
                request.CompanyEmail);

            _dbContext.Tenants.Add(tenant);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            //
            // 2. Create Owner Identity account
            //

            var owner = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,

                FirstName =
                    request.OwnerFirstName.Trim(),

                LastName =
                    request.OwnerLastName.Trim(),

                EmailConfirmed = false,
                IsActive = true
            };

            var userResult =
                await _userManager.CreateAsync(
                    owner,
                    request.Password);

            if (!userResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    userResult.Errors.Select(
                        x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to create tenant owner: {errors}");
            }

            //
            // 3. Create tenant membership
            //

            var tenantUser = new TenantUser(
                tenant.Id,
                owner.Id,
                isDefaultTenant: true);

            _dbContext.TenantUsers.Add(
                tenantUser);

            //
            // 4. Create default tenant roles
            //

            var roles =
                CreateDefaultRoles(
                    tenant.Id);

            _dbContext.TenantRoles.AddRange(
                roles);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            //
            // 5. Assign permissions
            //

            await AssignDefaultPermissionsAsync(
                roles,
                cancellationToken);

            //
            // 6. Assign owner role
            //

            var ownerRole =
                roles.Single(
                    x => x.Code ==
                         TenantRoleCodes.TenantOwner);

            var tenantUserRole =
                new TenantUserRole(
                    tenantUser.Id,
                    ownerRole.Id);

            _dbContext.TenantUserRoles.Add(
                tenantUserRole);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            //
            // 7. Commit everything
            //

            await transaction.CommitAsync(
                cancellationToken);

            return new CreateTenantResult(
                tenant.Id,
                owner.Id,
                tenantUser.Id,
                tenant.Code,
                owner.Email!);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static List<TenantRole> CreateDefaultRoles(
        Guid tenantId)
    {
        return
        [
            new TenantRole(
                tenantId,
                "Tenant Owner",
                TenantRoleCodes.TenantOwner,
                "Full administrative control of the tenant.",
                true),

            new TenantRole(
                tenantId,
                "Company Admin",
                TenantRoleCodes.CompanyAdmin,
                "Manages company users and configuration.",
                true),

            new TenantRole(
                tenantId,
                "Fleet Manager",
                TenantRoleCodes.FleetManager,
                "Manages fleet vehicles and drivers.",
                true),

            new TenantRole(
                tenantId,
                "Dispatcher",
                TenantRoleCodes.Dispatcher,
                "Handles vehicle and driver operations.",
                true),

            new TenantRole(
                tenantId,
                "Maintenance Manager",
                TenantRoleCodes.MaintenanceManager,
                "Manages fleet maintenance operations.",
                true),

            new TenantRole(
                tenantId,
                "Safety Officer",
                TenantRoleCodes.SafetyOfficer,
                "Manages fleet safety activities.",
                true),

            new TenantRole(
                tenantId,
                "Asset Manager",
                TenantRoleCodes.AssetManager,
                "Manages company assets and equipment.",
                true),

            new TenantRole(
                tenantId,
                "Driver",
                TenantRoleCodes.Driver,
                "Fleet driver access.",
                true)
        ];
    }

    private async Task AssignDefaultPermissionsAsync(
        IReadOnlyCollection<TenantRole> roles,
        CancellationToken cancellationToken)
    {
        var permissions =
            await _dbContext.Permissions
                .Where(x => x.IsActive)
                .ToListAsync(
                    cancellationToken);

        var ownerRole =
            roles.Single(
                x => x.Code ==
                     TenantRoleCodes.TenantOwner);

        //
        // Owner gets every current permission.
        //

        foreach (var permission in permissions)
        {
            _dbContext.RolePermissions.Add(
                new RolePermission(
                    ownerRole.Id,
                    permission.Id));
        }

        AssignPermissions(
            roles,
            permissions,
            TenantRoleCodes.CompanyAdmin,
            [
                PermissionKeys.Tenants.View,

                PermissionKeys.Users.View,
                PermissionKeys.Users.Create,
                PermissionKeys.Users.Edit,
                PermissionKeys.Users.ManageRoles,

                PermissionKeys.Organization.View,
                PermissionKeys.Organization.Manage,
                PermissionKeys.Branches.View,
                PermissionKeys.Branches.Manage,
                PermissionKeys.Locations.View,
                PermissionKeys.Locations.Manage,
                PermissionKeys.Departments.View,
                PermissionKeys.Departments.Manage,
                PermissionKeys.Teams.View,
                PermissionKeys.Teams.Manage,
                PermissionKeys.Employees.View,
                PermissionKeys.Employees.Create,
                PermissionKeys.Employees.Edit,
                PermissionKeys.Employees.Delete,
                PermissionKeys.OrganizationDashboard.View,

                PermissionKeys.Vehicles.View,
                PermissionKeys.Vehicles.Create,
                PermissionKeys.Vehicles.Edit,
                PermissionKeys.Vehicles.AssignDriver,
                PermissionKeys.Vehicles.ManageStatus,
                PermissionKeys.Vehicles.ManageOdometer,
                PermissionKeys.Vehicles.ManageDocuments,
                PermissionKeys.Vehicles.ManageNotes,
                PermissionKeys.VehicleCategories.View,
                PermissionKeys.VehicleCategories.Manage,
                PermissionKeys.VehicleMakes.View,
                PermissionKeys.VehicleMakes.Manage,
                PermissionKeys.VehicleModels.View,
                PermissionKeys.VehicleModels.Manage,
                PermissionKeys.VehicleDashboard.View,

                PermissionKeys.Drivers.View,
                PermissionKeys.Drivers.Create,
                PermissionKeys.Drivers.Edit,
                PermissionKeys.Drivers.Delete,
                PermissionKeys.Drivers.ChangeStatus,
                PermissionKeys.Drivers.ManageOrganization,
                PermissionKeys.Drivers.LinkEmployee,
                PermissionKeys.Drivers.AssignVehicle,
                PermissionKeys.DriverLicenses.View,
                PermissionKeys.DriverLicenses.Manage,
                PermissionKeys.DriverCertifications.View,
                PermissionKeys.DriverCertifications.Manage,
                PermissionKeys.DriverDocuments.View,
                PermissionKeys.DriverDocuments.Manage,
                PermissionKeys.DriverAssignments.View,
                PermissionKeys.DriverAssignments.Create,
                PermissionKeys.DriverAssignments.End,
                PermissionKeys.DriverDashboard.View,

                PermissionKeys.Assets.View,
                PermissionKeys.Assets.Create,
                PermissionKeys.Assets.Edit,

                PermissionKeys.Reports.View,
                PermissionKeys.Reports.Export,

                PermissionKeys.FleetOperations.View,
                PermissionKeys.FleetOperations.ManageOperations,
                PermissionKeys.FleetOperations.Checkout,
                PermissionKeys.FleetOperations.CheckIn,
                PermissionKeys.FleetOperations.Handover,
                PermissionKeys.FleetOperations.ManageShifts,
                PermissionKeys.FleetOperations.AssignShifts,
                PermissionKeys.FleetOperations.ManageExceptions,
                PermissionKeys.FleetOperations.ViewBoard,
                PermissionKeys.FleetOperations.ViewTimeline,

                PermissionKeys.TrackingDevices.View,
                PermissionKeys.TrackingDevices.Create,
                PermissionKeys.TrackingDevices.Edit,
                PermissionKeys.TrackingDevices.Delete,
                PermissionKeys.TrackingDevices.Provision,
                PermissionKeys.TrackingDevices.AssignVehicle,

                PermissionKeys.Telematics.View,
                PermissionKeys.Telematics.ViewLive,
                PermissionKeys.Telematics.ViewHistory,
                PermissionKeys.Telematics.ViewDeviceHealth,
                PermissionKeys.Telematics.ViewDashboard,
                PermissionKeys.Telematics.SendCommands,

                PermissionKeys.Maps.View,
                PermissionKeys.Maps.ViewLive,
                PermissionKeys.Maps.ViewHistory,

                PermissionKeys.Geofences.View,
                PermissionKeys.Geofences.Create,
                PermissionKeys.Geofences.Edit,
                PermissionKeys.Geofences.Delete,
                PermissionKeys.Geofences.ViewEvents,

                PermissionKeys.DemoFleet.View,
                PermissionKeys.DemoFleet.Manage
            ]);

        AssignPermissions(
            roles,
            permissions,
            TenantRoleCodes.FleetManager,
            [
                PermissionKeys.Vehicles.View,
                PermissionKeys.Vehicles.Create,
                PermissionKeys.Vehicles.Edit,
                PermissionKeys.Vehicles.AssignDriver,
                PermissionKeys.Vehicles.ManageStatus,
                PermissionKeys.Vehicles.ManageOdometer,
                PermissionKeys.Vehicles.ManageDocuments,
                PermissionKeys.Vehicles.ManageNotes,
                PermissionKeys.VehicleCategories.View,
                PermissionKeys.VehicleCategories.Manage,
                PermissionKeys.VehicleMakes.View,
                PermissionKeys.VehicleMakes.Manage,
                PermissionKeys.VehicleModels.View,
                PermissionKeys.VehicleModels.Manage,
                PermissionKeys.VehicleDashboard.View,

                PermissionKeys.Drivers.View,
                PermissionKeys.Drivers.Create,
                PermissionKeys.Drivers.Edit,
                PermissionKeys.Drivers.Delete,
                PermissionKeys.Drivers.ChangeStatus,
                PermissionKeys.Drivers.ManageOrganization,
                PermissionKeys.Drivers.LinkEmployee,
                PermissionKeys.Drivers.AssignVehicle,
                PermissionKeys.DriverLicenses.View,
                PermissionKeys.DriverLicenses.Manage,
                PermissionKeys.DriverCertifications.View,
                PermissionKeys.DriverCertifications.Manage,
                PermissionKeys.DriverDocuments.View,
                PermissionKeys.DriverDocuments.Manage,
                PermissionKeys.DriverAssignments.View,
                PermissionKeys.DriverAssignments.Create,
                PermissionKeys.DriverAssignments.End,
                PermissionKeys.DriverDashboard.View,

                PermissionKeys.Reports.View,

                PermissionKeys.FleetOperations.View,
                PermissionKeys.FleetOperations.ManageOperations,
                PermissionKeys.FleetOperations.Checkout,
                PermissionKeys.FleetOperations.CheckIn,
                PermissionKeys.FleetOperations.Handover,
                PermissionKeys.FleetOperations.ManageShifts,
                PermissionKeys.FleetOperations.AssignShifts,
                PermissionKeys.FleetOperations.ManageExceptions,
                PermissionKeys.FleetOperations.ViewBoard,
                PermissionKeys.FleetOperations.ViewTimeline,

                PermissionKeys.TrackingDevices.View,
                PermissionKeys.TrackingDevices.Create,
                PermissionKeys.TrackingDevices.Edit,
                PermissionKeys.TrackingDevices.Delete,
                PermissionKeys.TrackingDevices.Provision,
                PermissionKeys.TrackingDevices.AssignVehicle,

                PermissionKeys.Telematics.View,
                PermissionKeys.Telematics.ViewLive,
                PermissionKeys.Telematics.ViewHistory,
                PermissionKeys.Telematics.ViewDeviceHealth,
                PermissionKeys.Telematics.ViewDashboard,

                PermissionKeys.Maps.View,
                PermissionKeys.Maps.ViewLive,
                PermissionKeys.Maps.ViewHistory,

                PermissionKeys.Geofences.View,
                PermissionKeys.Geofences.Create,
                PermissionKeys.Geofences.Edit,
                PermissionKeys.Geofences.Delete,
                PermissionKeys.Geofences.ViewEvents,

                PermissionKeys.DemoFleet.View,
                PermissionKeys.DemoFleet.Manage
            ]);

        AssignPermissions(
            roles,
            permissions,
            TenantRoleCodes.Dispatcher,
            [
                PermissionKeys.Vehicles.View,
                PermissionKeys.Vehicles.AssignDriver,

                PermissionKeys.Drivers.View,
                PermissionKeys.Drivers.AssignVehicle,
                PermissionKeys.DriverAssignments.View,
                PermissionKeys.DriverAssignments.Create,
                PermissionKeys.DriverAssignments.End,
                PermissionKeys.DriverDashboard.View,

                PermissionKeys.FleetOperations.View,
                PermissionKeys.FleetOperations.ManageOperations,
                PermissionKeys.FleetOperations.Checkout,
                PermissionKeys.FleetOperations.CheckIn,
                PermissionKeys.FleetOperations.Handover,
                PermissionKeys.FleetOperations.AssignShifts,
                PermissionKeys.FleetOperations.ViewBoard,
                PermissionKeys.FleetOperations.ViewTimeline,

                PermissionKeys.Telematics.View,
                PermissionKeys.Telematics.ViewLive,
                PermissionKeys.Telematics.ViewDashboard,

                PermissionKeys.Maps.View,
                PermissionKeys.Maps.ViewLive,

                PermissionKeys.Geofences.View,
                PermissionKeys.Geofences.ViewEvents
            ]);

        AssignPermissions(
            roles,
            permissions,
            TenantRoleCodes.AssetManager,
            [
                PermissionKeys.Assets.View,
                PermissionKeys.Assets.Create,
                PermissionKeys.Assets.Edit,

                PermissionKeys.Reports.View,

                PermissionKeys.TrackingDevices.View,
                PermissionKeys.TrackingDevices.AssignVehicle,
                PermissionKeys.Telematics.ViewDeviceHealth
            ]);

        AssignPermissions(
            roles,
            permissions,
            TenantRoleCodes.Driver,
            [
                PermissionKeys.Vehicles.View,
                PermissionKeys.FleetOperations.View,
                PermissionKeys.FleetOperations.Checkout,
                PermissionKeys.FleetOperations.CheckIn,
                PermissionKeys.FleetOperations.Handover
            ]);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private void AssignPermissions(
        IReadOnlyCollection<TenantRole> roles,
        IReadOnlyCollection<Permission> permissions,
        string roleCode,
        IReadOnlyCollection<string> permissionKeys)
    {
        var role =
            roles.Single(
                x => x.Code == roleCode);

        var selectedPermissions =
            permissions
                .Where(
                    x => permissionKeys.Contains(
                        x.Key))
                .ToList();

        foreach (var permission in selectedPermissions)
        {
            _dbContext.RolePermissions.Add(
                new RolePermission(
                    role.Id,
                    permission.Id));
        }
    }

    private static void ValidateRequest(
        CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.CompanyName))
        {
            throw new ArgumentException(
                "Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.CompanyCode))
        {
            throw new ArgumentException(
                "Company code is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.OwnerFirstName))
        {
            throw new ArgumentException(
                "Owner first name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.OwnerEmail))
        {
            throw new ArgumentException(
                "Owner email is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Password))
        {
            throw new ArgumentException(
                "Password is required.");
        }
    }
}
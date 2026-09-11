using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Auth;
using TenantSettingsEntity = ConnectedOps.Domain.Tenancy.TenantSettings;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Accounting;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Security;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Infrastructure.Persistence;


public sealed class ConnectedOpsDbContext
    : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>
{
    public ConnectedOpsDbContext(
        DbContextOptions<ConnectedOpsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformSettings> PlatformSettings =>
        Set<PlatformSettings>();

    public DbSet<TenantInvitation> TenantInvitations
    => Set<TenantInvitation>();

    public DbSet<AuditLog> AuditLogs =>
    Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens
    => Set<RefreshToken>();

    public DbSet<SecurityLog> SecurityLogs =>
    Set<SecurityLog>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    public DbSet<Permission> Permissions =>
    Set<Permission>();

    public DbSet<TenantRole> TenantRoles =>
        Set<TenantRole>();

    public DbSet<TenantSettingsEntity> TenantSettings =>
    Set<TenantSettingsEntity>();

    public DbSet<TenantUserRole> TenantUserRoles =>
        Set<TenantUserRole>();

    public DbSet<RolePermission> RolePermissions =>
        Set<RolePermission>();

    public DbSet<OrganizationProfile> OrganizationProfiles =>
        Set<OrganizationProfile>();

    public DbSet<Branch> Branches =>
        Set<Branch>();

    public DbSet<Location> Locations =>
        Set<Location>();

    public DbSet<Department> Departments =>
        Set<Department>();

    public DbSet<Team> Teams =>
        Set<Team>();

    public DbSet<Employee> Employees =>
        Set<Employee>();

    public DbSet<OrganizationSettings> OrganizationSettings =>
        Set<OrganizationSettings>();

    public DbSet<SubscriptionPlan> SubscriptionPlans =>
        Set<SubscriptionPlan>();

    public DbSet<TenantSubscription> TenantSubscriptions =>
        Set<TenantSubscription>();

    public DbSet<Invoice> Invoices =>
        Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems =>
        Set<InvoiceItem>();

    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    public DbSet<GeneralLedgerAccount> GeneralLedgerAccounts =>
        Set<GeneralLedgerAccount>();

    public DbSet<LedgerEntry> LedgerEntries =>
        Set<LedgerEntry>();

    public DbSet<Vehicle> Vehicles =>
        Set<Vehicle>();

    public DbSet<VehicleCategory> VehicleCategories =>
        Set<VehicleCategory>();

    public DbSet<VehicleMake> VehicleMakes =>
        Set<VehicleMake>();

    public DbSet<VehicleModel> VehicleModels =>
        Set<VehicleModel>();

    public DbSet<VehicleSpecification> VehicleSpecifications =>
        Set<VehicleSpecification>();

    public DbSet<VehicleRegistration> VehicleRegistrations =>
        Set<VehicleRegistration>();

    public DbSet<VehicleOdometerEntry> VehicleOdometerEntries =>
        Set<VehicleOdometerEntry>();

    public DbSet<VehicleDocument> VehicleDocuments =>
        Set<VehicleDocument>();

    public DbSet<VehicleNote> VehicleNotes =>
        Set<VehicleNote>();

    public DbSet<Driver> Drivers =>
        Set<Driver>();

    public DbSet<DriverLicense> DriverLicenses =>
        Set<DriverLicense>();

    public DbSet<DriverLicenseCategory> DriverLicenseCategories =>
        Set<DriverLicenseCategory>();

    public DbSet<DriverCertification> DriverCertifications =>
        Set<DriverCertification>();

    public DbSet<DriverVehicleAssignment> DriverVehicleAssignments =>
        Set<DriverVehicleAssignment>();

    public DbSet<DriverDocument> DriverDocuments =>
        Set<DriverDocument>();

    public DbSet<DriverEmergencyContact> DriverEmergencyContacts =>
        Set<DriverEmergencyContact>();

    public DbSet<DriverNote> DriverNotes =>
        Set<DriverNote>();

    public DbSet<FleetShift> FleetShifts =>
        Set<FleetShift>();

    public DbSet<FleetShiftAssignment> FleetShiftAssignments =>
        Set<FleetShiftAssignment>();

    public DbSet<VehicleUsageSession> VehicleUsageSessions =>
        Set<VehicleUsageSession>();

    public DbSet<VehicleHandover> VehicleHandovers =>
        Set<VehicleHandover>();

    public DbSet<VehicleConditionRecord> VehicleConditionRecords =>
        Set<VehicleConditionRecord>();

    public DbSet<FleetOperationalException> FleetOperationalExceptions =>
        Set<FleetOperationalException>();

    public DbSet<TrackingProvider> TrackingProviders =>
        Set<TrackingProvider>();

    public DbSet<TrackingDeviceType> TrackingDeviceTypes =>
        Set<TrackingDeviceType>();

    public DbSet<TrackingDevice> TrackingDevices =>
        Set<TrackingDevice>();

    public DbSet<DeviceProvisioningRecord> DeviceProvisioningRecords =>
        Set<DeviceProvisioningRecord>();

    public DbSet<TrackingDeviceVehicleAssignment> TrackingDeviceVehicleAssignments =>
        Set<TrackingDeviceVehicleAssignment>();

    public DbSet<TelematicsSettings> TelematicsSettings =>
        Set<TelematicsSettings>();

    public DbSet<TelematicsProviderConfiguration> TelematicsProviderConfigurations =>
        Set<TelematicsProviderConfiguration>();

    public DbSet<TelemetryRecord> TelemetryRecords =>
        Set<TelemetryRecord>();

    public DbSet<VehicleTelemetryState> VehicleTelemetryStates =>
        Set<VehicleTelemetryState>();

    public DbSet<DeviceCommand> DeviceCommands =>
        Set<DeviceCommand>();

    public DbSet<TelemetryIngestionFailure> TelemetryIngestionFailures =>
        Set<TelemetryIngestionFailure>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConnectedOpsDbContext).Assembly);
    }
}
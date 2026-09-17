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
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Demo;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Reports;
using ConnectedOps.Domain.Dispatch;
using ConnectedOps.Domain.Alerts;
using ConnectedOps.Domain.Inspections;
using ConnectedOps.Domain.TollsAndFines;
using ConnectedOps.Domain.ColdChain;

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

    public DbSet<Geofence> Geofences =>
        Set<Geofence>();

    public DbSet<GeofenceEvent> GeofenceEvents =>
        Set<GeofenceEvent>();

    public DbSet<VehicleGeofenceState> VehicleGeofenceStates =>
        Set<VehicleGeofenceState>();

    public DbSet<DemoRoute> DemoRoutes =>
        Set<DemoRoute>();

    public DbSet<DemoRoutePoint> DemoRoutePoints =>
        Set<DemoRoutePoint>();

    public DbSet<MaintenanceServiceType> MaintenanceServiceTypes =>
        Set<MaintenanceServiceType>();

    public DbSet<MaintenanceProvider> MaintenanceProviders =>
        Set<MaintenanceProvider>();

    public DbSet<MaintenancePlan> MaintenancePlans =>
        Set<MaintenancePlan>();

    public DbSet<MaintenancePlanRule> MaintenancePlanRules =>
        Set<MaintenancePlanRule>();

    public DbSet<VehicleMaintenancePlanAssignment> VehicleMaintenancePlanAssignments =>
        Set<VehicleMaintenancePlanAssignment>();

    public DbSet<VehicleMaintenanceDue> VehicleMaintenanceDues =>
        Set<VehicleMaintenanceDue>();

    public DbSet<VehicleMaintenanceRecord> VehicleMaintenanceRecords =>
        Set<VehicleMaintenanceRecord>();

    public DbSet<VehicleMaintenanceTask> VehicleMaintenanceTasks =>
        Set<VehicleMaintenanceTask>();

    public DbSet<VehicleMaintenancePart> VehicleMaintenanceParts =>
        Set<VehicleMaintenancePart>();

    public DbSet<VehicleMaintenanceLabour> VehicleMaintenanceLabours =>
        Set<VehicleMaintenanceLabour>();

    public DbSet<VehicleMaintenanceExpense> VehicleMaintenanceExpenses =>
        Set<VehicleMaintenanceExpense>();

    public DbSet<VehicleDowntimeRecord> VehicleDowntimeRecords =>
        Set<VehicleDowntimeRecord>();

    public DbSet<VehicleMaintenanceDocument> VehicleMaintenanceDocuments =>
        Set<VehicleMaintenanceDocument>();

    public DbSet<FuelTypeDefinition> FuelTypeDefinitions =>
        Set<FuelTypeDefinition>();

    public DbSet<FuelStation> FuelStations =>
        Set<FuelStation>();

    public DbSet<FuelCard> FuelCards =>
        Set<FuelCard>();

    public DbSet<FuelTransaction> FuelTransactions =>
        Set<FuelTransaction>();

    public DbSet<FuelTransactionDocument> FuelTransactionDocuments =>
        Set<FuelTransactionDocument>();

    public DbSet<FuelImportBatch> FuelImportBatches =>
        Set<FuelImportBatch>();

    public DbSet<FuelImportError> FuelImportErrors =>
        Set<FuelImportError>();

    public DbSet<FuelAnomaly> FuelAnomalies =>
        Set<FuelAnomaly>();

    public DbSet<AssetCategory> AssetCategories =>
        Set<AssetCategory>();

    public DbSet<AssetType> AssetTypes =>
        Set<AssetType>();

    public DbSet<Asset> Assets =>
        Set<Asset>();

    public DbSet<AssetLocationHistory> AssetLocationHistories =>
        Set<AssetLocationHistory>();

    public DbSet<AssetEmployeeAssignment> AssetEmployeeAssignments =>
        Set<AssetEmployeeAssignment>();

    public DbSet<AssetVehicleAssignment> AssetVehicleAssignments =>
        Set<AssetVehicleAssignment>();

    public DbSet<AssetTransfer> AssetTransfers =>
        Set<AssetTransfer>();

    public DbSet<AssetUsageSession> AssetUsageSessions =>
        Set<AssetUsageSession>();

    public DbSet<AssetConditionRecord> AssetConditionRecords =>
        Set<AssetConditionRecord>();

    public DbSet<AssetInspection> AssetInspections =>
        Set<AssetInspection>();

    public DbSet<AssetInspectionItem> AssetInspectionItems =>
        Set<AssetInspectionItem>();

    public DbSet<AssetCalibrationRecord> AssetCalibrationRecords =>
        Set<AssetCalibrationRecord>();

    public DbSet<AssetDocument> AssetDocuments =>
        Set<AssetDocument>();

    public DbSet<AssetIdentifier> AssetIdentifiers =>
        Set<AssetIdentifier>();

    public DbSet<AssetNote> AssetNotes =>
        Set<AssetNote>();

    public DbSet<ComplianceRequirement> ComplianceRequirements =>
        Set<ComplianceRequirement>();

    public DbSet<ComplianceRequirementRule> ComplianceRequirementRules =>
        Set<ComplianceRequirementRule>();

    public DbSet<ComplianceRecord> ComplianceRecords =>
        Set<ComplianceRecord>();

    public DbSet<ComplianceDocument> ComplianceDocuments =>
        Set<ComplianceDocument>();

    public DbSet<ComplianceException> ComplianceExceptions =>
        Set<ComplianceException>();

    public DbSet<SafetyIncident> SafetyIncidents =>
        Set<SafetyIncident>();

    public DbSet<SafetyIncidentParticipant> SafetyIncidentParticipants =>
        Set<SafetyIncidentParticipant>();

    public DbSet<SafetyIncidentVehicle> SafetyIncidentVehicles =>
        Set<SafetyIncidentVehicle>();

    public DbSet<SafetyIncidentAsset> SafetyIncidentAssets =>
        Set<SafetyIncidentAsset>();

    public DbSet<SafetyIncidentEvidence> SafetyIncidentEvidence =>
        Set<SafetyIncidentEvidence>();

    public DbSet<SafetyIncidentInvestigation> SafetyIncidentInvestigations =>
        Set<SafetyIncidentInvestigation>();

    public DbSet<SafetyViolation> SafetyViolations =>
        Set<SafetyViolation>();

    public DbSet<CorrectiveAction> CorrectiveActions =>
        Set<CorrectiveAction>();

    public DbSet<ReportDefinition> ReportDefinitions =>
        Set<ReportDefinition>();

    public DbSet<ReportExecutionLog> ReportExecutionLogs =>
        Set<ReportExecutionLog>();

    public DbSet<EsgEmissionFactor> EsgEmissionFactors =>
        Set<EsgEmissionFactor>();

    public DbSet<DispatchJob> DispatchJobs =>
        Set<DispatchJob>();

    public DbSet<DispatchRoute> DispatchRoutes =>
        Set<DispatchRoute>();

    public DbSet<DispatchRouteStop> DispatchRouteStops =>
        Set<DispatchRouteStop>();

    public DbSet<ProofOfDelivery> ProofOfDeliveries =>
        Set<ProofOfDelivery>();

    // Phase 14 - Operational Intelligence Suite
    public DbSet<AlertRule> AlertRules =>
        Set<AlertRule>();

    public DbSet<AlertRuleCondition> AlertRuleConditions =>
        Set<AlertRuleCondition>();

    public DbSet<Alert> Alerts =>
        Set<Alert>();

    public DbSet<NotificationMessage> NotificationMessages =>
        Set<NotificationMessage>();

    public DbSet<DvirInspection> DvirInspections =>
        Set<DvirInspection>();

    public DbSet<DvirItemCheck> DvirItemChecks =>
        Set<DvirItemCheck>();

    public DbSet<TollTransaction> TollTransactions =>
        Set<TollTransaction>();

    public DbSet<TrafficViolation> TrafficViolations =>
        Set<TrafficViolation>();

    public DbSet<CargoSensorDevice> CargoSensorDevices =>
        Set<CargoSensorDevice>();

    public DbSet<CargoTelemetryReading> CargoTelemetryReadings =>
        Set<CargoTelemetryReading>();

    public DbSet<ColdChainExcursion> ColdChainExcursions =>
        Set<ColdChainExcursion>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConnectedOpsDbContext).Assembly);
    }
}
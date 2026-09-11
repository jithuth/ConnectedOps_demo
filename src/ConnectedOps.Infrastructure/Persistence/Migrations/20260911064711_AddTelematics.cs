using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelematics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelematicsSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfflineThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    TelemetryRetentionDays = table.Column<int>(type: "int", nullable: false),
                    MaxAcceptedFutureMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxAcceptedPastDays = table.Column<int>(type: "int", nullable: false),
                    OdometerUpdateThresholdKm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    OdometerUpdateMinIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelematicsSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryIngestionFailures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryIngestionFailures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingDeviceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportsGps = table.Column<bool>(type: "bit", nullable: false),
                    SupportsIgnition = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCanBus = table.Column<bool>(type: "bit", nullable: false),
                    SupportsObd = table.Column<bool>(type: "bit", nullable: false),
                    SupportsBattery = table.Column<bool>(type: "bit", nullable: false),
                    SupportsTemperature = table.Column<bool>(type: "bit", nullable: false),
                    SupportsFuel = table.Column<bool>(type: "bit", nullable: false),
                    SupportsBle = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCommands = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingDeviceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelematicsProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecretReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelematicsProviderConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelematicsProviderConfigurations_TrackingProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "TrackingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrackingDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IMEI = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SIMNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SIMICCID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTelemetryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastKnownLatitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownLongitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownSpeedKph = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    LastKnownHeadingDegrees = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    LastKnownIgnition = table.Column<bool>(type: "bit", nullable: true),
                    BatteryLevelPercent = table.Column<int>(type: "int", nullable: true),
                    ExternalPowerVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    BatteryVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    SignalStrength = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingDevices_TrackingDeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "TrackingDeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackingDevices_TrackingProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "TrackingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandType = table.Column<int>(type: "int", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCommands_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceProvisioningRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvisioningStatus = table.Column<int>(type: "int", nullable: false),
                    ProvisionedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProvisionedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceProvisioningRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceProvisioningRecords_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    AltitudeMeters = table.Column<double>(type: "float", nullable: true),
                    SpeedKph = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    HeadingDegrees = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IgnitionOn = table.Column<bool>(type: "bit", nullable: true),
                    OdometerKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EngineHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    FuelLevelPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FuelVolumeLiters = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    BatteryVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ExternalPowerVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    GsmSignal = table.Column<int>(type: "int", nullable: true),
                    GpsSatellites = table.Column<int>(type: "int", nullable: true),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DigitalInput1 = table.Column<bool>(type: "bit", nullable: true),
                    DigitalInput2 = table.Column<bool>(type: "bit", nullable: true),
                    RawEventCode = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    SourceProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryRecords_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelemetryRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrackingDeviceVehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingDeviceVehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingDeviceVehicleAssignments_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackingDeviceVehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTelemetryStates",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AltitudeMeters = table.Column<double>(type: "float", nullable: true),
                    SpeedKph = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    HeadingDegrees = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IgnitionOn = table.Column<bool>(type: "bit", nullable: true),
                    OdometerKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EngineHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    FuelLevelPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BatteryVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ExternalPowerVoltage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    SignalStrength = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTelemetryStates", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_VehicleTelemetryStates_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleTelemetryStates_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_TenantId_Status",
                table: "DeviceCommands",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_TenantId_TrackingDeviceId",
                table: "DeviceCommands",
                columns: new[] { "TenantId", "TrackingDeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_TrackingDeviceId",
                table: "DeviceCommands",
                column: "TrackingDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceProvisioningRecords_TenantId_ProvisioningStatus",
                table: "DeviceProvisioningRecords",
                columns: new[] { "TenantId", "ProvisioningStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceProvisioningRecords_TenantId_TrackingDeviceId",
                table: "DeviceProvisioningRecords",
                columns: new[] { "TenantId", "TrackingDeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceProvisioningRecords_TrackingDeviceId",
                table: "DeviceProvisioningRecords",
                column: "TrackingDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TelematicsProviderConfigurations_ProviderId",
                table: "TelematicsProviderConfigurations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TelematicsProviderConfigurations_TenantId_ProviderId",
                table: "TelematicsProviderConfigurations",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TelematicsSettings_TenantId",
                table: "TelematicsSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryIngestionFailures_DeviceIdentifier",
                table: "TelemetryIngestionFailures",
                column: "DeviceIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryIngestionFailures_TenantId_ReceivedAtUtc",
                table: "TelemetryIngestionFailures",
                columns: new[] { "TenantId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_TenantId_ProviderMessageId",
                table: "TelemetryRecords",
                columns: new[] { "TenantId", "ProviderMessageId" },
                filter: "[ProviderMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_TenantId_RecordedAtUtc",
                table: "TelemetryRecords",
                columns: new[] { "TenantId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_TenantId_TrackingDeviceId_RecordedAtUtc",
                table: "TelemetryRecords",
                columns: new[] { "TenantId", "TrackingDeviceId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_TenantId_VehicleId_RecordedAtUtc",
                table: "TelemetryRecords",
                columns: new[] { "TenantId", "VehicleId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_TrackingDeviceId_RecordedAtUtc",
                table: "TelemetryRecords",
                columns: new[] { "TrackingDeviceId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_VehicleId",
                table: "TelemetryRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_DeviceTypeId",
                table: "TrackingDevices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_ProviderId",
                table: "TrackingDevices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_TenantId_DeviceIdentifier",
                table: "TrackingDevices",
                columns: new[] { "TenantId", "DeviceIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_TenantId_IMEI",
                table: "TrackingDevices",
                columns: new[] { "TenantId", "IMEI" },
                unique: true,
                filter: "[IMEI] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_TenantId_LastSeenAtUtc",
                table: "TrackingDevices",
                columns: new[] { "TenantId", "LastSeenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_TenantId_ProviderId",
                table: "TrackingDevices",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDevices_TenantId_Status",
                table: "TrackingDevices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceTypes_Code",
                table: "TrackingDeviceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceTypes_TenantId",
                table: "TrackingDeviceTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceVehicleAssignments_TenantId_TrackingDeviceId_IsActive",
                table: "TrackingDeviceVehicleAssignments",
                columns: new[] { "TenantId", "TrackingDeviceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceVehicleAssignments_TenantId_VehicleId_IsActive",
                table: "TrackingDeviceVehicleAssignments",
                columns: new[] { "TenantId", "VehicleId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceVehicleAssignments_TenantId_VehicleId_IsPrimary_IsActive",
                table: "TrackingDeviceVehicleAssignments",
                columns: new[] { "TenantId", "VehicleId", "IsPrimary", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceVehicleAssignments_TrackingDeviceId",
                table: "TrackingDeviceVehicleAssignments",
                column: "TrackingDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDeviceVehicleAssignments_VehicleId",
                table: "TrackingDeviceVehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingProviders_Code",
                table: "TrackingProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingProviders_TenantId",
                table: "TrackingProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetryStates_TenantId_TrackingDeviceId",
                table: "VehicleTelemetryStates",
                columns: new[] { "TenantId", "TrackingDeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetryStates_TenantId_VehicleId",
                table: "VehicleTelemetryStates",
                columns: new[] { "TenantId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetryStates_TrackingDeviceId",
                table: "VehicleTelemetryStates",
                column: "TrackingDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCommands");

            migrationBuilder.DropTable(
                name: "DeviceProvisioningRecords");

            migrationBuilder.DropTable(
                name: "TelematicsProviderConfigurations");

            migrationBuilder.DropTable(
                name: "TelematicsSettings");

            migrationBuilder.DropTable(
                name: "TelemetryIngestionFailures");

            migrationBuilder.DropTable(
                name: "TelemetryRecords");

            migrationBuilder.DropTable(
                name: "TrackingDeviceVehicleAssignments");

            migrationBuilder.DropTable(
                name: "VehicleTelemetryStates");

            migrationBuilder.DropTable(
                name: "TrackingDevices");

            migrationBuilder.DropTable(
                name: "TrackingDeviceTypes");

            migrationBuilder.DropTable(
                name: "TrackingProviders");
        }
    }
}

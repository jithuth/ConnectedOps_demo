using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase14OperationalIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoSensorDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorTagNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinTargetTemperatureCelsius = table.Column<double>(type: "float", nullable: false),
                    MaxTargetTemperatureCelsius = table.Column<double>(type: "float", nullable: false),
                    BatteryLevelPercent = table.Column<int>(type: "int", nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CurrentTemperatureCelsius = table.Column<double>(type: "float", nullable: true),
                    CurrentHumidityPercent = table.Column<double>(type: "float", nullable: true),
                    CurrentDoorOpen = table.Column<bool>(type: "bit", nullable: false),
                    LastReadingAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoSensorDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoSensorDevices_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DvirInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Odometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DriverSignatureData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MechanicName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MechanicNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MechanicSignatureData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DvirInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DvirInspections_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DvirInspections_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TollTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TollSystem = table.Column<int>(type: "int", nullable: false),
                    TollGateName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TollGateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MatchedDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedUsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TollTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TollTransactions_Drivers_MatchedDriverId",
                        column: x => x.MatchedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TollTransactions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrafficViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuthorityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ViolationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BlackPoints = table.Column<int>(type: "int", nullable: false),
                    ViolationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedUsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiabilityStatus = table.Column<int>(type: "int", nullable: false),
                    DisputeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficViolations_Drivers_MatchedDriverId",
                        column: x => x.MatchedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrafficViolations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertRuleConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operator = table.Column<int>(type: "int", nullable: false),
                    ThresholdValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRuleConditions_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    TriggerDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CargoTelemetryReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargoSensorDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemperatureCelsius = table.Column<double>(type: "float", nullable: false),
                    HumidityPercent = table.Column<double>(type: "float", nullable: true),
                    DoorOpen = table.Column<bool>(type: "bit", nullable: false),
                    ReeferMode = table.Column<int>(type: "int", nullable: false),
                    SetpointTemperatureCelsius = table.Column<double>(type: "float", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoTelemetryReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoTelemetryReadings_CargoSensorDevices_CargoSensorDeviceId",
                        column: x => x.CargoSensorDeviceId,
                        principalTable: "CargoSensorDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColdChainExcursions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargoSensorDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BreachTemperatureCelsius = table.Column<double>(type: "float", nullable: false),
                    AllowableMinCelsius = table.Column<double>(type: "float", nullable: false),
                    AllowableMaxCelsius = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColdChainExcursions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColdChainExcursions_CargoSensorDevices_CargoSensorDeviceId",
                        column: x => x.CargoSensorDeviceId,
                        principalTable: "CargoSensorDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColdChainExcursions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DvirItemChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DvirInspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: true),
                    DefectDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DvirItemChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DvirItemChecks_DvirInspections_DvirInspectionId",
                        column: x => x.DvirInspectionId,
                        principalTable: "DvirInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMessages_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRuleConditions_AlertRuleId",
                table: "AlertRuleConditions",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRuleConditions_TenantId_AlertRuleId",
                table: "AlertRuleConditions",
                columns: new[] { "TenantId", "AlertRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_TenantId_Code",
                table: "AlertRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_TenantId_IsEnabled",
                table: "AlertRules",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DriverId",
                table: "Alerts",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_Severity",
                table: "Alerts",
                columns: new[] { "TenantId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_Status",
                table: "Alerts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_TriggeredAtUtc",
                table: "Alerts",
                columns: new[] { "TenantId", "TriggeredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_VehicleId",
                table: "Alerts",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_VehicleId",
                table: "Alerts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoSensorDevices_TenantId_SensorTagNumber",
                table: "CargoSensorDevices",
                columns: new[] { "TenantId", "SensorTagNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CargoSensorDevices_TenantId_VehicleId",
                table: "CargoSensorDevices",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_CargoSensorDevices_VehicleId",
                table: "CargoSensorDevices",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoTelemetryReadings_CargoSensorDeviceId",
                table: "CargoTelemetryReadings",
                column: "CargoSensorDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoTelemetryReadings_TenantId_CargoSensorDeviceId_RecordedAtUtc",
                table: "CargoTelemetryReadings",
                columns: new[] { "TenantId", "CargoSensorDeviceId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ColdChainExcursions_CargoSensorDeviceId",
                table: "ColdChainExcursions",
                column: "CargoSensorDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ColdChainExcursions_TenantId_StartedAtUtc",
                table: "ColdChainExcursions",
                columns: new[] { "TenantId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ColdChainExcursions_TenantId_Status",
                table: "ColdChainExcursions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ColdChainExcursions_TenantId_VehicleId",
                table: "ColdChainExcursions",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ColdChainExcursions_VehicleId",
                table: "ColdChainExcursions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_DriverId",
                table: "DvirInspections",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_TenantId_DriverId",
                table: "DvirInspections",
                columns: new[] { "TenantId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_TenantId_InspectionNumber",
                table: "DvirInspections",
                columns: new[] { "TenantId", "InspectionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_TenantId_Status",
                table: "DvirInspections",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_TenantId_VehicleId",
                table: "DvirInspections",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_DvirInspections_VehicleId",
                table: "DvirInspections",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DvirItemChecks_DvirInspectionId",
                table: "DvirItemChecks",
                column: "DvirInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DvirItemChecks_TenantId_DvirInspectionId",
                table: "DvirItemChecks",
                columns: new[] { "TenantId", "DvirInspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_AlertId",
                table: "NotificationMessages",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_TenantId_Channel",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_TenantId_SentAtUtc",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TollTransactions_MatchedDriverId",
                table: "TollTransactions",
                column: "MatchedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TollTransactions_TenantId_MatchedDriverId",
                table: "TollTransactions",
                columns: new[] { "TenantId", "MatchedDriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_TollTransactions_TenantId_TransactionTimeUtc",
                table: "TollTransactions",
                columns: new[] { "TenantId", "TransactionTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TollTransactions_TenantId_VehicleId",
                table: "TollTransactions",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TollTransactions_VehicleId",
                table: "TollTransactions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_MatchedDriverId",
                table: "TrafficViolations",
                column: "MatchedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_TenantId_LiabilityStatus",
                table: "TrafficViolations",
                columns: new[] { "TenantId", "LiabilityStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_TenantId_MatchedDriverId",
                table: "TrafficViolations",
                columns: new[] { "TenantId", "MatchedDriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_TenantId_TicketNumber",
                table: "TrafficViolations",
                columns: new[] { "TenantId", "TicketNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_TenantId_VehicleId",
                table: "TrafficViolations",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_VehicleId",
                table: "TrafficViolations",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRuleConditions");

            migrationBuilder.DropTable(
                name: "CargoTelemetryReadings");

            migrationBuilder.DropTable(
                name: "ColdChainExcursions");

            migrationBuilder.DropTable(
                name: "DvirItemChecks");

            migrationBuilder.DropTable(
                name: "NotificationMessages");

            migrationBuilder.DropTable(
                name: "TollTransactions");

            migrationBuilder.DropTable(
                name: "TrafficViolations");

            migrationBuilder.DropTable(
                name: "CargoSensorDevices");

            migrationBuilder.DropTable(
                name: "DvirInspections");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "AlertRules");
        }
    }
}

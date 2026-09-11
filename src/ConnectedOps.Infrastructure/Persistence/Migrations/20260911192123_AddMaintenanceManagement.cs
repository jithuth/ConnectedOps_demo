using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VehicleCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_MaintenancePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenancePlans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenancePlans_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_MaintenanceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceProviders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceServiceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DefaultDurationHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_MaintenanceServiceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceServiceTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenancePlanAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenancePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaselineEngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenancePlanAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenancePlanAssignments_MaintenancePlans_MaintenancePlanId",
                        column: x => x.MaintenancePlanId,
                        principalTable: "MaintenancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenancePlanAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePlanRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenancePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceServiceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    IntervalKilometers = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IntervalMiles = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IntervalEngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: true),
                    IntervalMonths = table.Column<int>(type: "int", nullable: true),
                    InitialDueKilometers = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InitialDueEngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InitialDueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderBeforeKilometers = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReminderBeforeEngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReminderBeforeDays = table.Column<int>(type: "int", nullable: true),
                    ToleranceKilometers = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToleranceHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToleranceDays = table.Column<int>(type: "int", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlanRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenancePlanRules_MaintenancePlans_MaintenancePlanId",
                        column: x => x.MaintenancePlanId,
                        principalTable: "MaintenancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenancePlanRules_MaintenanceServiceTypes_MaintenanceServiceTypeId",
                        column: x => x.MaintenanceServiceTypeId,
                        principalTable: "MaintenanceServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenancePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaintenancePlanRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaintenanceServiceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OdometerReading = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OdometerUnit = table.Column<int>(type: "int", nullable: false),
                    EngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaintenanceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TechnicianNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalPartsCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalLabourCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OtherCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VehicleDowntimeMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_MaintenancePlanRules_MaintenancePlanRuleId",
                        column: x => x.MaintenancePlanRuleId,
                        principalTable: "MaintenancePlanRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_MaintenancePlans_MaintenancePlanId",
                        column: x => x.MaintenancePlanId,
                        principalTable: "MaintenancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_MaintenanceProviders_MaintenanceProviderId",
                        column: x => x.MaintenanceProviderId,
                        principalTable: "MaintenanceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_MaintenanceServiceTypes_MaintenanceServiceTypeId",
                        column: x => x.MaintenanceServiceTypeId,
                        principalTable: "MaintenanceServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDowntimeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    DowntimeType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
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
                    table.PrimaryKey("PK_VehicleDowntimeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDowntimeRecords_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VehicleDowntimeRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceDocuments_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceDues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenancePlanRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastMaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextDueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextDueOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NextDueEngineHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DueStatus = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceDues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceDues_MaintenancePlanRules_MaintenancePlanRuleId",
                        column: x => x.MaintenancePlanRuleId,
                        principalTable: "MaintenancePlanRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceDues_VehicleMaintenanceRecords_LastMaintenanceRecordId",
                        column: x => x.LastMaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceDues_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceExpenses_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceLabours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TechnicianName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceLabours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceLabours_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceLabours_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PartName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceParts_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceServiceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenanceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceTasks_MaintenanceServiceTypes_MaintenanceServiceTypeId",
                        column: x => x.MaintenanceServiceTypeId,
                        principalTable: "MaintenanceServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleMaintenanceTasks_VehicleMaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "VehicleMaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlanRules_MaintenancePlanId",
                table: "MaintenancePlanRules",
                column: "MaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlanRules_MaintenanceServiceTypeId",
                table: "MaintenancePlanRules",
                column: "MaintenanceServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlanRules_TenantId_MaintenancePlanId",
                table: "MaintenancePlanRules",
                columns: new[] { "TenantId", "MaintenancePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlanRules_TenantId_MaintenanceServiceTypeId",
                table: "MaintenancePlanRules",
                columns: new[] { "TenantId", "MaintenanceServiceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_TenantId_Code",
                table: "MaintenancePlans",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_TenantId_IsActive",
                table: "MaintenancePlans",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_TenantId_VehicleCategoryId",
                table: "MaintenancePlans",
                columns: new[] { "TenantId", "VehicleCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_VehicleCategoryId",
                table: "MaintenancePlans",
                column: "VehicleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceProviders_BranchId",
                table: "MaintenanceProviders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceProviders_TenantId_BranchId",
                table: "MaintenanceProviders",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceProviders_TenantId_Code",
                table: "MaintenanceProviders",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceProviders_TenantId_IsActive",
                table: "MaintenanceProviders",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceProviders_TenantId_ProviderType",
                table: "MaintenanceProviders",
                columns: new[] { "TenantId", "ProviderType" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceServiceTypes_TenantId_Category",
                table: "MaintenanceServiceTypes",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceServiceTypes_TenantId_Code",
                table: "MaintenanceServiceTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceServiceTypes_TenantId_IsActive",
                table: "MaintenanceServiceTypes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDowntimeRecords_MaintenanceRecordId",
                table: "VehicleDowntimeRecords",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDowntimeRecords_TenantId_MaintenanceRecordId",
                table: "VehicleDowntimeRecords",
                columns: new[] { "TenantId", "MaintenanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDowntimeRecords_TenantId_VehicleId_StartedAtUtc",
                table: "VehicleDowntimeRecords",
                columns: new[] { "TenantId", "VehicleId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDowntimeRecords_VehicleId",
                table: "VehicleDowntimeRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDocuments_MaintenanceRecordId",
                table: "VehicleMaintenanceDocuments",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDocuments_TenantId_MaintenanceRecordId",
                table: "VehicleMaintenanceDocuments",
                columns: new[] { "TenantId", "MaintenanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_LastMaintenanceRecordId",
                table: "VehicleMaintenanceDues",
                column: "LastMaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_MaintenancePlanRuleId",
                table: "VehicleMaintenanceDues",
                column: "MaintenancePlanRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_TenantId_DueStatus",
                table: "VehicleMaintenanceDues",
                columns: new[] { "TenantId", "DueStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_TenantId_NextDueDateUtc",
                table: "VehicleMaintenanceDues",
                columns: new[] { "TenantId", "NextDueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_TenantId_VehicleId_MaintenancePlanRuleId",
                table: "VehicleMaintenanceDues",
                columns: new[] { "TenantId", "VehicleId", "MaintenancePlanRuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceDues_VehicleId",
                table: "VehicleMaintenanceDues",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceExpenses_MaintenanceRecordId",
                table: "VehicleMaintenanceExpenses",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceExpenses_TenantId_MaintenanceRecordId",
                table: "VehicleMaintenanceExpenses",
                columns: new[] { "TenantId", "MaintenanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceLabours_EmployeeId",
                table: "VehicleMaintenanceLabours",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceLabours_MaintenanceRecordId",
                table: "VehicleMaintenanceLabours",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceLabours_TenantId_EmployeeId",
                table: "VehicleMaintenanceLabours",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceLabours_TenantId_MaintenanceRecordId",
                table: "VehicleMaintenanceLabours",
                columns: new[] { "TenantId", "MaintenanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceParts_MaintenanceRecordId",
                table: "VehicleMaintenanceParts",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceParts_TenantId_MaintenanceRecordId",
                table: "VehicleMaintenanceParts",
                columns: new[] { "TenantId", "MaintenanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenancePlanAssignments_MaintenancePlanId",
                table: "VehicleMaintenancePlanAssignments",
                column: "MaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenancePlanAssignments_TenantId_MaintenancePlanId",
                table: "VehicleMaintenancePlanAssignments",
                columns: new[] { "TenantId", "MaintenancePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenancePlanAssignments_TenantId_VehicleId_IsActive",
                table: "VehicleMaintenancePlanAssignments",
                columns: new[] { "TenantId", "VehicleId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenancePlanAssignments_VehicleId",
                table: "VehicleMaintenancePlanAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_MaintenancePlanId",
                table: "VehicleMaintenanceRecords",
                column: "MaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_MaintenancePlanRuleId",
                table: "VehicleMaintenanceRecords",
                column: "MaintenancePlanRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_MaintenanceProviderId",
                table: "VehicleMaintenanceRecords",
                column: "MaintenanceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_MaintenanceServiceTypeId",
                table: "VehicleMaintenanceRecords",
                column: "MaintenanceServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_TenantId_MaintenanceProviderId",
                table: "VehicleMaintenanceRecords",
                columns: new[] { "TenantId", "MaintenanceProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_TenantId_MaintenanceServiceTypeId_ServiceDateUtc",
                table: "VehicleMaintenanceRecords",
                columns: new[] { "TenantId", "MaintenanceServiceTypeId", "ServiceDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_TenantId_Status",
                table: "VehicleMaintenanceRecords",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_TenantId_VehicleId_ServiceDateUtc",
                table: "VehicleMaintenanceRecords",
                columns: new[] { "TenantId", "VehicleId", "ServiceDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceRecords_VehicleId",
                table: "VehicleMaintenanceRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceTasks_MaintenanceRecordId",
                table: "VehicleMaintenanceTasks",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceTasks_MaintenanceServiceTypeId",
                table: "VehicleMaintenanceTasks",
                column: "MaintenanceServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMaintenanceTasks_TenantId_MaintenanceRecordId",
                table: "VehicleMaintenanceTasks",
                columns: new[] { "TenantId", "MaintenanceRecordId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleDowntimeRecords");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceDocuments");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceDues");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceExpenses");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceLabours");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceParts");

            migrationBuilder.DropTable(
                name: "VehicleMaintenancePlanAssignments");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceTasks");

            migrationBuilder.DropTable(
                name: "VehicleMaintenanceRecords");

            migrationBuilder.DropTable(
                name: "MaintenancePlanRules");

            migrationBuilder.DropTable(
                name: "MaintenanceProviders");

            migrationBuilder.DropTable(
                name: "MaintenancePlans");

            migrationBuilder.DropTable(
                name: "MaintenanceServiceTypes");
        }
    }
}

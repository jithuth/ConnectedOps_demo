using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceAndSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplianceRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppliesTo = table.Column<int>(type: "int", nullable: false),
                    RequirementType = table.Column<int>(type: "int", nullable: false),
                    ValidityType = table.Column<int>(type: "int", nullable: false),
                    DefaultValidityDays = table.Column<int>(type: "int", nullable: true),
                    DefaultReminderDays = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ComplianceRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncidentType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImmediateActionTaken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvestigationRequired = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceExceptions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceExceptions_ComplianceRequirements_ComplianceRequirementId",
                        column: x => x.ComplianceRequirementId,
                        principalTable: "ComplianceRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceExceptions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceExceptions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_ComplianceRequirements_ComplianceRequirementId",
                        column: x => x.ComplianceRequirementId,
                        principalTable: "ComplianceRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceRequirementRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverType = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ComplianceRequirementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRequirementRules_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComplianceRequirementRules_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComplianceRequirementRules_ComplianceRequirements_ComplianceRequirementId",
                        column: x => x.ComplianceRequirementId,
                        principalTable: "ComplianceRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceRequirementRules_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidentAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamageReported = table.Column<bool>(type: "bit", nullable: false),
                    DamageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyIncidentAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentAssets_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidentEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_SafetyIncidentEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentEvidence_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidentInvestigations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigatorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RootCause = table.Column<int>(type: "int", nullable: false),
                    RootCauseDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContributingFactors = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyIncidentInvestigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentInvestigations_Employees_InvestigatorEmployeeId",
                        column: x => x.InvestigatorEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentInvestigations_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidentParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantType = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InjuryReported = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SafetyIncidentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentParticipants_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentParticipants_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentParticipants_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyIncidentVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamageReported = table.Column<bool>(type: "bit", nullable: false),
                    DamageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsPrimaryVehicle = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyIncidentVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentVehicles_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SafetyIncidentVehicles_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SafetyViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViolationType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SafetyViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyViolations_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyViolations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyViolations_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyViolations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IssueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ComplianceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceDocuments_ComplianceRecords_ComplianceRecordId",
                        column: x => x.ComplianceRecordId,
                        principalTable: "ComplianceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorrectiveActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SafetyIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SafetyViolationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComplianceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationRequired = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectiveActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_ComplianceRecords_ComplianceRecordId",
                        column: x => x.ComplianceRecordId,
                        principalTable: "ComplianceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_SafetyIncidents_SafetyIncidentId",
                        column: x => x.SafetyIncidentId,
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_SafetyViolations_SafetyViolationId",
                        column: x => x.SafetyViolationId,
                        principalTable: "SafetyViolations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDocuments_ComplianceRecordId",
                table: "ComplianceDocuments",
                column: "ComplianceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDocuments_TenantId_ComplianceRecordId",
                table: "ComplianceDocuments",
                columns: new[] { "TenantId", "ComplianceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_AssetId",
                table: "ComplianceExceptions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_ComplianceRequirementId",
                table: "ComplianceExceptions",
                column: "ComplianceRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_DriverId",
                table: "ComplianceExceptions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_TenantId_ComplianceRequirementId",
                table: "ComplianceExceptions",
                columns: new[] { "TenantId", "ComplianceRequirementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_TenantId_SubjectType_Status",
                table: "ComplianceExceptions",
                columns: new[] { "TenantId", "SubjectType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceExceptions_VehicleId",
                table: "ComplianceExceptions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_AssetId",
                table: "ComplianceRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_ComplianceRequirementId",
                table: "ComplianceRecords",
                column: "ComplianceRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_DriverId",
                table: "ComplianceRecords",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_AssetId",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_DriverId",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_ExpiryDateUtc",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "ExpiryDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_Status",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_SubjectType",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "SubjectType" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_TenantId_VehicleId",
                table: "ComplianceRecords",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_VehicleId",
                table: "ComplianceRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirementRules_AssetCategoryId",
                table: "ComplianceRequirementRules",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirementRules_BranchId",
                table: "ComplianceRequirementRules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirementRules_ComplianceRequirementId",
                table: "ComplianceRequirementRules",
                column: "ComplianceRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirementRules_TenantId_ComplianceRequirementId",
                table: "ComplianceRequirementRules",
                columns: new[] { "TenantId", "ComplianceRequirementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirementRules_VehicleCategoryId",
                table: "ComplianceRequirementRules",
                column: "VehicleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirements_TenantId_AppliesTo_IsActive",
                table: "ComplianceRequirements",
                columns: new[] { "TenantId", "AppliesTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirements_TenantId_Code",
                table: "ComplianceRequirements",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_AssignedEmployeeId",
                table: "CorrectiveActions",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_ComplianceRecordId",
                table: "CorrectiveActions",
                column: "ComplianceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_SafetyIncidentId",
                table: "CorrectiveActions",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_SafetyViolationId",
                table: "CorrectiveActions",
                column: "SafetyViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_TenantId_AssignedEmployeeId",
                table: "CorrectiveActions",
                columns: new[] { "TenantId", "AssignedEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_TenantId_SafetyIncidentId",
                table: "CorrectiveActions",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_TenantId_Status_DueDateUtc",
                table: "CorrectiveActions",
                columns: new[] { "TenantId", "Status", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentAssets_AssetId",
                table: "SafetyIncidentAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentAssets_SafetyIncidentId",
                table: "SafetyIncidentAssets",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentAssets_TenantId_AssetId",
                table: "SafetyIncidentAssets",
                columns: new[] { "TenantId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentAssets_TenantId_SafetyIncidentId",
                table: "SafetyIncidentAssets",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentEvidence_SafetyIncidentId",
                table: "SafetyIncidentEvidence",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentEvidence_TenantId_SafetyIncidentId",
                table: "SafetyIncidentEvidence",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentInvestigations_InvestigatorEmployeeId",
                table: "SafetyIncidentInvestigations",
                column: "InvestigatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentInvestigations_SafetyIncidentId",
                table: "SafetyIncidentInvestigations",
                column: "SafetyIncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentInvestigations_TenantId_SafetyIncidentId",
                table: "SafetyIncidentInvestigations",
                columns: new[] { "TenantId", "SafetyIncidentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentParticipants_DriverId",
                table: "SafetyIncidentParticipants",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentParticipants_EmployeeId",
                table: "SafetyIncidentParticipants",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentParticipants_SafetyIncidentId",
                table: "SafetyIncidentParticipants",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentParticipants_TenantId_SafetyIncidentId",
                table: "SafetyIncidentParticipants",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_BranchId",
                table: "SafetyIncidents",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_LocationId",
                table: "SafetyIncidents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_TenantId_BranchId",
                table: "SafetyIncidents",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_TenantId_IncidentNumber",
                table: "SafetyIncidents",
                columns: new[] { "TenantId", "IncidentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_TenantId_Severity",
                table: "SafetyIncidents",
                columns: new[] { "TenantId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_TenantId_Status_OccurredAtUtc",
                table: "SafetyIncidents",
                columns: new[] { "TenantId", "Status", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentVehicles_SafetyIncidentId",
                table: "SafetyIncidentVehicles",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentVehicles_TenantId_SafetyIncidentId",
                table: "SafetyIncidentVehicles",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentVehicles_TenantId_VehicleId",
                table: "SafetyIncidentVehicles",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidentVehicles_VehicleId",
                table: "SafetyIncidentVehicles",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_DriverId",
                table: "SafetyViolations",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_EmployeeId",
                table: "SafetyViolations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_SafetyIncidentId",
                table: "SafetyViolations",
                column: "SafetyIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_TenantId_DriverId",
                table: "SafetyViolations",
                columns: new[] { "TenantId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_TenantId_IsResolved_OccurredAtUtc",
                table: "SafetyViolations",
                columns: new[] { "TenantId", "IsResolved", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_TenantId_SafetyIncidentId",
                table: "SafetyViolations",
                columns: new[] { "TenantId", "SafetyIncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_TenantId_VehicleId",
                table: "SafetyViolations",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyViolations_VehicleId",
                table: "SafetyViolations",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceDocuments");

            migrationBuilder.DropTable(
                name: "ComplianceExceptions");

            migrationBuilder.DropTable(
                name: "ComplianceRequirementRules");

            migrationBuilder.DropTable(
                name: "CorrectiveActions");

            migrationBuilder.DropTable(
                name: "SafetyIncidentAssets");

            migrationBuilder.DropTable(
                name: "SafetyIncidentEvidence");

            migrationBuilder.DropTable(
                name: "SafetyIncidentInvestigations");

            migrationBuilder.DropTable(
                name: "SafetyIncidentParticipants");

            migrationBuilder.DropTable(
                name: "SafetyIncidentVehicles");

            migrationBuilder.DropTable(
                name: "ComplianceRecords");

            migrationBuilder.DropTable(
                name: "SafetyViolations");

            migrationBuilder.DropTable(
                name: "ComplianceRequirements");

            migrationBuilder.DropTable(
                name: "SafetyIncidents");
        }
    }
}

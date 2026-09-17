using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase15DriverExperienceAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    EarnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverBadges_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    SafetyScore = table.Column<double>(type: "float", nullable: false),
                    EcoScore = table.Column<double>(type: "float", nullable: false),
                    ComplianceScore = table.Column<double>(type: "float", nullable: false),
                    DispatchScore = table.Column<double>(type: "float", nullable: false),
                    HarshBrakingCount = table.Column<int>(type: "int", nullable: false),
                    HarshAccelerationCount = table.Column<int>(type: "int", nullable: false),
                    SpeedingEventsCount = table.Column<int>(type: "int", nullable: false),
                    IdleHours = table.Column<double>(type: "float", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    RankInFleet = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverScorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverScorecards_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverTripExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DispatchJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReceiptImageKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IncurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReimbursementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripExpenses_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverTripExpenses_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HosLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EndOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCertified = table.Column<bool>(type: "bit", nullable: false),
                    CertifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HosLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HosLogEntries_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HosLogEntries_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HosRuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresetType = table.Column<int>(type: "int", nullable: false),
                    MaxDrivingHoursPerShift = table.Column<double>(type: "float", nullable: false),
                    MaxShiftDutyHours = table.Column<double>(type: "float", nullable: false),
                    DriveHoursBeforeMandatoryBreak = table.Column<double>(type: "float", nullable: false),
                    MandatoryBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    MinConsecutiveOffDutyHours = table.Column<double>(type: "float", nullable: false),
                    CycleDays = table.Column<int>(type: "int", nullable: false),
                    CycleMaxDutyHours = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HosRuleConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HosViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViolationType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HosViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HosViolations_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicTrackingTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    FeedbackComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicTrackingTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicTrackingTokens_DispatchJobs_DispatchJobId",
                        column: x => x.DispatchJobId,
                        principalTable: "DispatchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverBadges_DriverId",
                table: "DriverBadges",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverBadges_TenantId_DriverId_BadgeType",
                table: "DriverBadges",
                columns: new[] { "TenantId", "DriverId", "BadgeType" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverScorecards_DriverId",
                table: "DriverScorecards",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverScorecards_TenantId_DriverId_PeriodYear_PeriodMonth",
                table: "DriverScorecards",
                columns: new[] { "TenantId", "DriverId", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripExpenses_DriverId",
                table: "DriverTripExpenses",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripExpenses_TenantId_DriverId_IncurredAtUtc",
                table: "DriverTripExpenses",
                columns: new[] { "TenantId", "DriverId", "IncurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripExpenses_VehicleId",
                table: "DriverTripExpenses",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_HosLogEntries_DriverId",
                table: "HosLogEntries",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_HosLogEntries_TenantId_DriverId_StartedAtUtc",
                table: "HosLogEntries",
                columns: new[] { "TenantId", "DriverId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HosLogEntries_VehicleId",
                table: "HosLogEntries",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_HosRuleConfigurations_TenantId",
                table: "HosRuleConfigurations",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HosViolations_DriverId",
                table: "HosViolations",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_HosViolations_TenantId_DriverId_OccurredAtUtc",
                table: "HosViolations",
                columns: new[] { "TenantId", "DriverId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicTrackingTokens_DispatchJobId",
                table: "PublicTrackingTokens",
                column: "DispatchJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicTrackingTokens_TenantId_DispatchJobId",
                table: "PublicTrackingTokens",
                columns: new[] { "TenantId", "DispatchJobId" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicTrackingTokens_Token",
                table: "PublicTrackingTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverBadges");

            migrationBuilder.DropTable(
                name: "DriverScorecards");

            migrationBuilder.DropTable(
                name: "DriverTripExpenses");

            migrationBuilder.DropTable(
                name: "HosLogEntries");

            migrationBuilder.DropTable(
                name: "HosRuleConfigurations");

            migrationBuilder.DropTable(
                name: "HosViolations");

            migrationBuilder.DropTable(
                name: "PublicTrackingTokens");
        }
    }
}

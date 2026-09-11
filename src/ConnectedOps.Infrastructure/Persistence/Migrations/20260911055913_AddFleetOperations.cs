using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FleetShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CrossesMidnight = table.Column<bool>(type: "bit", nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FleetShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FleetShifts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FleetShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FleetShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignmentStatus = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FleetShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetShiftAssignments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FleetShiftAssignments_FleetShifts_FleetShiftId",
                        column: x => x.FleetShiftId,
                        principalTable: "FleetShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetShiftAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FleetShiftAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleUsageSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FleetShiftAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedOutByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdometerUnit = table.Column<int>(type: "int", nullable: false),
                    StartLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckoutCondition = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedInByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndOdometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EndLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckInCondition = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_VehicleUsageSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleUsageSessions_DriverVehicleAssignments_DriverVehicleAssignmentId",
                        column: x => x.DriverVehicleAssignmentId,
                        principalTable: "DriverVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VehicleUsageSessions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleUsageSessions_FleetShiftAssignments_FleetShiftAssignmentId",
                        column: x => x.FleetShiftAssignmentId,
                        principalTable: "FleetShiftAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VehicleUsageSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleUsageSessions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FleetOperationalExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExceptionType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_FleetOperationalExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetOperationalExceptions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FleetOperationalExceptions_VehicleUsageSessions_UsageSessionId",
                        column: x => x.UsageSessionId,
                        principalTable: "VehicleUsageSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FleetOperationalExceptions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleConditionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Odometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleConditionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleConditionRecords_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleConditionRecords_VehicleUsageSessions_UsageSessionId",
                        column: x => x.UsageSessionId,
                        principalTable: "VehicleUsageSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VehicleConditionRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleHandovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToUsageSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Odometer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdometerUnit = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AcknowledgedByFromDriver = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedByToDriver = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_Drivers_FromDriverId",
                        column: x => x.FromDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_Drivers_ToDriverId",
                        column: x => x.ToDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_VehicleUsageSessions_FromUsageSessionId",
                        column: x => x.FromUsageSessionId,
                        principalTable: "VehicleUsageSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_VehicleUsageSessions_ToUsageSessionId",
                        column: x => x.ToUsageSessionId,
                        principalTable: "VehicleUsageSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleHandovers_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_DriverId",
                table: "FleetOperationalExceptions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_TenantId",
                table: "FleetOperationalExceptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_TenantId_DriverId",
                table: "FleetOperationalExceptions",
                columns: new[] { "TenantId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_TenantId_Status_OccurredAtUtc",
                table: "FleetOperationalExceptions",
                columns: new[] { "TenantId", "Status", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_TenantId_VehicleId",
                table: "FleetOperationalExceptions",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_UsageSessionId",
                table: "FleetOperationalExceptions",
                column: "UsageSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetOperationalExceptions_VehicleId",
                table: "FleetOperationalExceptions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_DriverId",
                table: "FleetShiftAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_FleetShiftId",
                table: "FleetShiftAssignments",
                column: "FleetShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_TenantId_AssignmentStatus",
                table: "FleetShiftAssignments",
                columns: new[] { "TenantId", "AssignmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_TenantId_DriverId_StartDateTimeUtc",
                table: "FleetShiftAssignments",
                columns: new[] { "TenantId", "DriverId", "StartDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_TenantId_FleetShiftId_AssignmentDate",
                table: "FleetShiftAssignments",
                columns: new[] { "TenantId", "FleetShiftId", "AssignmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_TenantId_VehicleId_StartDateTimeUtc",
                table: "FleetShiftAssignments",
                columns: new[] { "TenantId", "VehicleId", "StartDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetShiftAssignments_VehicleId",
                table: "FleetShiftAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetShifts_BranchId",
                table: "FleetShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetShifts_TenantId_BranchId",
                table: "FleetShifts",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetShifts_TenantId_Code",
                table: "FleetShifts",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetShifts_TenantId_IsActive",
                table: "FleetShifts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_DriverId",
                table: "VehicleConditionRecords",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_TenantId",
                table: "VehicleConditionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_TenantId_DriverId",
                table: "VehicleConditionRecords",
                columns: new[] { "TenantId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_TenantId_VehicleId_RecordedAtUtc",
                table: "VehicleConditionRecords",
                columns: new[] { "TenantId", "VehicleId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_UsageSessionId",
                table: "VehicleConditionRecords",
                column: "UsageSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleConditionRecords_VehicleId",
                table: "VehicleConditionRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_FromDriverId",
                table: "VehicleHandovers",
                column: "FromDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_FromUsageSessionId",
                table: "VehicleHandovers",
                column: "FromUsageSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_TenantId_FromDriverId",
                table: "VehicleHandovers",
                columns: new[] { "TenantId", "FromDriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_TenantId_ToDriverId",
                table: "VehicleHandovers",
                columns: new[] { "TenantId", "ToDriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_TenantId_VehicleId_HandoverAtUtc",
                table: "VehicleHandovers",
                columns: new[] { "TenantId", "VehicleId", "HandoverAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_ToDriverId",
                table: "VehicleHandovers",
                column: "ToDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_ToUsageSessionId",
                table: "VehicleHandovers",
                column: "ToUsageSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleHandovers_VehicleId",
                table: "VehicleHandovers",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_DriverId",
                table: "VehicleUsageSessions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_DriverVehicleAssignmentId",
                table: "VehicleUsageSessions",
                column: "DriverVehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_FleetShiftAssignmentId",
                table: "VehicleUsageSessions",
                column: "FleetShiftAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_TenantId_CheckedInAtUtc",
                table: "VehicleUsageSessions",
                columns: new[] { "TenantId", "CheckedInAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_TenantId_CheckedOutAtUtc",
                table: "VehicleUsageSessions",
                columns: new[] { "TenantId", "CheckedOutAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_TenantId_DriverId",
                table: "VehicleUsageSessions",
                columns: new[] { "TenantId", "DriverId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_TenantId_Status",
                table: "VehicleUsageSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_TenantId_VehicleId",
                table: "VehicleUsageSessions",
                columns: new[] { "TenantId", "VehicleId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageSessions_VehicleId",
                table: "VehicleUsageSessions",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FleetOperationalExceptions");

            migrationBuilder.DropTable(
                name: "VehicleConditionRecords");

            migrationBuilder.DropTable(
                name: "VehicleHandovers");

            migrationBuilder.DropTable(
                name: "VehicleUsageSessions");

            migrationBuilder.DropTable(
                name: "FleetShiftAssignments");

            migrationBuilder.DropTable(
                name: "FleetShifts");
        }
    }
}

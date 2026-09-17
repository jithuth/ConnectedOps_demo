using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase17AiPredictiveFleetMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictiveMaintenanceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subsystem = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    FailureProbability = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EstimatedRulDays = table.Column<int>(type: "int", nullable: false),
                    ComponentTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SymptomDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EstimatedRepairCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PromotedMaintenanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkOrderCreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictiveMaintenanceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictiveMaintenanceAlerts_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleSubsystemHealths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subsystem = table.Column<int>(type: "int", nullable: false),
                    HealthScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Trend = table.Column<int>(type: "int", nullable: false),
                    AnomalyCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedRulDays = table.Column<int>(type: "int", nullable: true),
                    DiagnosticsNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleSubsystemHealths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleSubsystemHealths_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveMaintenanceAlerts_DetectedAtUtc",
                table: "PredictiveMaintenanceAlerts",
                column: "DetectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveMaintenanceAlerts_TenantId_RiskLevel",
                table: "PredictiveMaintenanceAlerts",
                columns: new[] { "TenantId", "RiskLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveMaintenanceAlerts_TenantId_Status",
                table: "PredictiveMaintenanceAlerts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveMaintenanceAlerts_TenantId_VehicleId",
                table: "PredictiveMaintenanceAlerts",
                columns: new[] { "TenantId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveMaintenanceAlerts_VehicleId",
                table: "PredictiveMaintenanceAlerts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubsystemHealths_TenantId_HealthScore",
                table: "VehicleSubsystemHealths",
                columns: new[] { "TenantId", "HealthScore" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubsystemHealths_TenantId_VehicleId_Subsystem",
                table: "VehicleSubsystemHealths",
                columns: new[] { "TenantId", "VehicleId", "Subsystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubsystemHealths_VehicleId",
                table: "VehicleSubsystemHealths",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictiveMaintenanceAlerts");

            migrationBuilder.DropTable(
                name: "VehicleSubsystemHealths");
        }
    }
}

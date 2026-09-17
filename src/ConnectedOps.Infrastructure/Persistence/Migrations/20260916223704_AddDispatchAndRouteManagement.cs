using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchAndRouteManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_DispatchRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRoutes_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchRoutes_Locations_EndLocationId",
                        column: x => x.EndLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchRoutes_Locations_StartLocationId",
                        column: x => x.StartLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchRoutes_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DispatchJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    TimeWindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeWindowEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VolumeM3 = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    PackageCount = table.Column<int>(type: "int", nullable: false),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_DispatchJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchJobs_DispatchRoutes_AssignedRouteId",
                        column: x => x.AssignedRouteId,
                        principalTable: "DispatchRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchJobs_Drivers_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchJobs_Vehicles_AssignedVehicleId",
                        column: x => x.AssignedVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlannedArrivalUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualArrivalUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDepartureUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DispatchRouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRouteStops_DispatchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DispatchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRouteStops_DispatchRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "DispatchRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProofOfDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationType = table.Column<int>(type: "int", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SignatureData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProofOfDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_DispatchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DispatchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_DispatchRouteStops_RouteStopId",
                        column: x => x.RouteStopId,
                        principalTable: "DispatchRouteStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_AssignedDriverId",
                table: "DispatchJobs",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_AssignedRouteId",
                table: "DispatchJobs",
                column: "AssignedRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_AssignedVehicleId",
                table: "DispatchJobs",
                column: "AssignedVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_TenantId_AssignedRouteId",
                table: "DispatchJobs",
                columns: new[] { "TenantId", "AssignedRouteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_TenantId_JobNumber",
                table: "DispatchJobs",
                columns: new[] { "TenantId", "JobNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchJobs_TenantId_Status",
                table: "DispatchJobs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_DriverId",
                table: "DispatchRoutes",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_EndLocationId",
                table: "DispatchRoutes",
                column: "EndLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_StartLocationId",
                table: "DispatchRoutes",
                column: "StartLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_TenantId_RouteNumber",
                table: "DispatchRoutes",
                columns: new[] { "TenantId", "RouteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_TenantId_ScheduledDate",
                table: "DispatchRoutes",
                columns: new[] { "TenantId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_TenantId_Status",
                table: "DispatchRoutes",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_VehicleId",
                table: "DispatchRoutes",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRouteStops_JobId",
                table: "DispatchRouteStops",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRouteStops_RouteId",
                table: "DispatchRouteStops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRouteStops_TenantId_JobId",
                table: "DispatchRouteStops",
                columns: new[] { "TenantId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRouteStops_TenantId_RouteId_SequenceOrder",
                table: "DispatchRouteStops",
                columns: new[] { "TenantId", "RouteId", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_JobId",
                table: "ProofOfDeliveries",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_RouteStopId",
                table: "ProofOfDeliveries",
                column: "RouteStopId",
                unique: true,
                filter: "[RouteStopId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_TenantId_JobId",
                table: "ProofOfDeliveries",
                columns: new[] { "TenantId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_TenantId_RouteStopId",
                table: "ProofOfDeliveries",
                columns: new[] { "TenantId", "RouteStopId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProofOfDeliveries");

            migrationBuilder.DropTable(
                name: "DispatchRouteStops");

            migrationBuilder.DropTable(
                name: "DispatchJobs");

            migrationBuilder.DropTable(
                name: "DispatchRoutes");
        }
    }
}

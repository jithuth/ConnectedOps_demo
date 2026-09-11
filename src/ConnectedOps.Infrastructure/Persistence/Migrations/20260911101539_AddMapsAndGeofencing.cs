using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectedOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMapsAndGeofencing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsLoop = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DemoRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Geofences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeofenceType = table.Column<int>(type: "int", nullable: false),
                    CenterLatitude = table.Column<double>(type: "float", nullable: true),
                    CenterLongitude = table.Column<double>(type: "float", nullable: true),
                    RadiusMeters = table.Column<double>(type: "float", nullable: true),
                    PolygonGeoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColorHex = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "#3B82F6"),
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
                    table.PrimaryKey("PK_Geofences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Geofences_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Geofences_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DemoRoutePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemoRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    SpeedKph = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoRoutePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoRoutePoints_DemoRoutes_DemoRouteId",
                        column: x => x.DemoRouteId,
                        principalTable: "DemoRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeofenceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeofenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TelemetryRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofenceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_Geofences_GeofenceId",
                        column: x => x.GeofenceId,
                        principalTable: "Geofences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_TelemetryRecords_TelemetryRecordId",
                        column: x => x.TelemetryRecordId,
                        principalTable: "TelemetryRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_TrackingDevices_TrackingDeviceId",
                        column: x => x.TrackingDeviceId,
                        principalTable: "TrackingDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleGeofenceStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeofenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsInside = table.Column<bool>(type: "bit", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEnteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastExitedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleGeofenceStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleGeofenceStates_Geofences_GeofenceId",
                        column: x => x.GeofenceId,
                        principalTable: "Geofences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleGeofenceStates_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoRoutePoints_DemoRouteId_Sequence",
                table: "DemoRoutePoints",
                columns: new[] { "DemoRouteId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoRoutes_TenantId_Name",
                table: "DemoRoutes",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_DriverId",
                table: "GeofenceEvents",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_GeofenceId",
                table: "GeofenceEvents",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_TelemetryRecordId",
                table: "GeofenceEvents",
                column: "TelemetryRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_TenantId_GeofenceId_OccurredAtUtc",
                table: "GeofenceEvents",
                columns: new[] { "TenantId", "GeofenceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_TenantId_OccurredAtUtc",
                table: "GeofenceEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_TenantId_VehicleId_OccurredAtUtc",
                table: "GeofenceEvents",
                columns: new[] { "TenantId", "VehicleId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_TrackingDeviceId",
                table: "GeofenceEvents",
                column: "TrackingDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_VehicleId",
                table: "GeofenceEvents",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_BranchId",
                table: "Geofences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_LocationId",
                table: "Geofences",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_TenantId_Code",
                table: "Geofences",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_TenantId_IsActive_IsDeleted",
                table: "Geofences",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeofenceStates_GeofenceId",
                table: "VehicleGeofenceStates",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeofenceStates_TenantId_GeofenceId_IsInside",
                table: "VehicleGeofenceStates",
                columns: new[] { "TenantId", "GeofenceId", "IsInside" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeofenceStates_TenantId_VehicleId_GeofenceId",
                table: "VehicleGeofenceStates",
                columns: new[] { "TenantId", "VehicleId", "GeofenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeofenceStates_TenantId_VehicleId_IsInside",
                table: "VehicleGeofenceStates",
                columns: new[] { "TenantId", "VehicleId", "IsInside" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeofenceStates_VehicleId",
                table: "VehicleGeofenceStates",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoRoutePoints");

            migrationBuilder.DropTable(
                name: "GeofenceEvents");

            migrationBuilder.DropTable(
                name: "VehicleGeofenceStates");

            migrationBuilder.DropTable(
                name: "DemoRoutes");

            migrationBuilder.DropTable(
                name: "Geofences");
        }
    }
}

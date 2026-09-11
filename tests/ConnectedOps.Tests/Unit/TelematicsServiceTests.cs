using System.Buffers.Binary;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Telematics;
using ConnectedOps.Infrastructure.Telematics.Providers;
using ConnectedOps.Infrastructure.Vehicles;
using ConnectedOps.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class TelematicsServiceTests
{
    private static async Task<(Vehicle Vehicle, TrackingProvider Provider, TrackingDeviceType DeviceType, TrackingDevice Device)>
        SeedTelematicsEnvironmentAsync(ConnectedOpsDbContext db, Guid tenantId)
    {
        // 1. Vehicle prerequisites
        var category = new VehicleCategory(tenantId, "Light Truck", "LTRUCK", null, true);
        var make = new VehicleMake(tenantId, "Volvo", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "FL Electric", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            "FLT-TEL-01",
            category.Id,
            make.Id,
            model.Id,
            displayName: "Volvo Delivery Truck",
            registrationNumber: "TEL-9988",
            currentOdometer: 5000m,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);

        // 2. Telematics Provider & Device Type
        var provider = new TrackingProvider("Teltonika Telematics", "TELTONIKA", ProviderType.Teltonika, "Hardware GPS manufacturer", tenantId);
        var deviceType = new TrackingDeviceType("FMC130 GPS Tracker", "FMC130", "Advanced 4G GPS Tracker", tenantId, supportsGps: true, supportsBattery: true, supportsCommands: true);
        db.TrackingProviders.Add(provider);
        db.TrackingDeviceTypes.Add(deviceType);
        await db.SaveChangesAsync();

        // 3. Tracking Device
        var device = new TrackingDevice(
            tenantId,
            "FMC-123456789",
            provider.Id,
            deviceType.Id,
            imei: "860123456789012",
            serialNumber: "SN-998877",
            name: "Truck 01 Tracker",
            status: TrackingDeviceStatus.Pending,
            isActive: true);
        db.TrackingDevices.Add(device);

        // 4. Default Telematics Settings
        var settings = new TelematicsSettings(
            tenantId,
            offlineThresholdMinutes: 5,
            telemetryRetentionDays: 90,
            maxAcceptedFutureMinutes: 15,
            maxAcceptedPastDays: 7,
            odometerUpdateThresholdKm: 1.0m);
        db.TelematicsSettings.Add(settings);

        await db.SaveChangesAsync();

        return (vehicle, provider, deviceType, device);
    }

    private static byte[] BuildTeltonikaCodec8Packet(
        DateTime timestamp,
        double latitude,
        double longitude,
        short altitude,
        ushort heading,
        byte satellites,
        ushort speed,
        bool ignition,
        int batteryPct,
        decimal batteryVoltageVolts)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // TCP Preamble: 4 zero bytes
        writer.Write(0);

        using var recordMs = new MemoryStream();
        using var rw = new BinaryWriter(recordMs);

        rw.Write((byte)0x08); // Codec ID: Codec 8
        rw.Write((byte)1);    // Record count: 1

        // Timestamp in ms (Big Endian)
        long tsMs = new DateTimeOffset(timestamp).ToUnixTimeMilliseconds();
        rw.Write(BinaryPrimitives.ReverseEndianness(tsMs));

        // Priority (1 byte)
        rw.Write((byte)0);

        // Coordinates (Big Endian)
        int lonRaw = (int)(longitude * 10000000);
        int latRaw = (int)(latitude * 10000000);
        rw.Write(BinaryPrimitives.ReverseEndianness(lonRaw));
        rw.Write(BinaryPrimitives.ReverseEndianness(latRaw));

        // Altitude (2 bytes, Big Endian)
        rw.Write(BinaryPrimitives.ReverseEndianness(altitude));

        // Heading / Angle (2 bytes, Big Endian)
        rw.Write(BinaryPrimitives.ReverseEndianness(heading));

        // Satellites (1 byte)
        rw.Write(satellites);

        // Speed (2 bytes, Big Endian)
        rw.Write(BinaryPrimitives.ReverseEndianness(speed));

        // Event IO ID (1 byte)
        rw.Write((byte)0);

        // Total IO count (1 byte)
        rw.Write((byte)3);

        // 1-byte elements count: 2 (Ignition ID 239, Battery % ID 113)
        rw.Write((byte)2);
        rw.Write((byte)239); // Ignition
        rw.Write((byte)(ignition ? 1 : 0));
        rw.Write((byte)113); // Battery %
        rw.Write((byte)batteryPct);

        // 2-byte elements count: 1 (Battery Voltage mV ID 67)
        rw.Write((byte)1);
        rw.Write((byte)67);
        ushort batMv = (ushort)(batteryVoltageVolts * 1000m);
        rw.Write(BinaryPrimitives.ReverseEndianness(batMv));

        // 4-byte elements count: 0
        rw.Write((byte)0);

        // 8-byte elements count: 0
        rw.Write((byte)0);

        // Number of Data 2 (1 byte)
        rw.Write((byte)1);

        var recordBytes = recordMs.ToArray();

        // Data Length (4 bytes, Big Endian)
        writer.Write(BinaryPrimitives.ReverseEndianness(recordBytes.Length));
        writer.Write(recordBytes);

        // CRC (4 bytes)
        writer.Write(0);

        return ms.ToArray();
    }

    #region 1. Device Master Tests

    [Fact]
    public async Task TrackingDeviceService_CreateDevice_SucceedsAndAudits()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceService = new TrackingDeviceService(db, userContext, connectivityService, auditService);

        var (_, provider, deviceType, _) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var request = new CreateTrackingDeviceRequest(
            DeviceIdentifier: "GPS-NEW-999",
            ProviderId: provider.Id,
            DeviceTypeId: deviceType.Id,
            IMEI: "860999000111222",
            SerialNumber: "SN-NEW-999",
            Name: "New Backup Tracker",
            Model: "FMC130",
            Manufacturer: "Teltonika",
            FirmwareVersion: "03.28.00.Rev.00",
            SIMNumber: "890123456789",
            SIMICCID: "ICCID998877",
            PhoneNumber: "+15550001");

        var result = await deviceService.CreateDeviceAsync(request);

        Assert.NotNull(result);
        Assert.Equal("GPS-NEW-999", result.DeviceIdentifier);
        Assert.Equal("860999000111222", result.IMEI);
        Assert.Equal(TrackingDeviceStatus.Pending, result.Status);
        Assert.True(result.IsActive);

        Assert.Single(auditService.WrittenLogs);
        Assert.Equal(AuditAction.TrackingDeviceCreated, auditService.WrittenLogs[0].Action);
    }

    [Fact]
    public async Task TrackingDeviceService_CreateDuplicateIdentifier_ThrowsException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceService = new TrackingDeviceService(db, userContext, connectivityService, auditService);

        var (_, provider, deviceType, existingDevice) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var request = new CreateTrackingDeviceRequest(
            DeviceIdentifier: existingDevice.DeviceIdentifier, // Duplicate identifier
            ProviderId: provider.Id,
            DeviceTypeId: deviceType.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => deviceService.CreateDeviceAsync(request));
    }

    [Fact]
    public async Task TrackingDeviceService_CreateDuplicateIMEI_ThrowsException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceService = new TrackingDeviceService(db, userContext, connectivityService, auditService);

        var (_, provider, deviceType, existingDevice) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var request = new CreateTrackingDeviceRequest(
            DeviceIdentifier: "GPS-DIFFERENT-ID",
            ProviderId: provider.Id,
            DeviceTypeId: deviceType.Id,
            IMEI: existingDevice.IMEI); // Duplicate IMEI

        await Assert.ThrowsAsync<InvalidOperationException>(() => deviceService.CreateDeviceAsync(request));
    }

    [Fact]
    public async Task TrackingDeviceService_UpdateDevice_UpdatesFieldsAndAudits()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceService = new TrackingDeviceService(db, userContext, connectivityService, auditService);

        var (_, provider, deviceType, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var updateRequest = new UpdateTrackingDeviceRequest(
            Name: "Updated Main Tracker",
            ProviderId: provider.Id,
            DeviceTypeId: deviceType.Id,
            IMEI: "860123456789012",
            SerialNumber: "SN-UPDATED-01",
            Model: "FMC130-Pro",
            Manufacturer: "Teltonika Telematics",
            FirmwareVersion: "03.29.00",
            SIMNumber: "890999888777",
            SIMICCID: "ICCID-UPDATED",
            PhoneNumber: "+15559999");

        var result = await deviceService.UpdateDeviceAsync(device.Id, updateRequest);

        Assert.Equal("Updated Main Tracker", result.Name);
        Assert.Equal("FMC130-Pro", result.Model);
        Assert.Single(auditService.WrittenLogs);
        Assert.Equal(AuditAction.TrackingDeviceUpdated, auditService.WrittenLogs[0].Action);
    }

    [Fact]
    public async Task TrackingDeviceService_ActivateAndDeactivate_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceService = new TrackingDeviceService(db, userContext, connectivityService, auditService);

        var (_, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        // Deactivate
        await deviceService.DeactivateDeviceAsync(device.Id);
        var deactivated = await deviceService.GetDeviceByIdAsync(device.Id);
        Assert.NotNull(deactivated);
        Assert.False(deactivated.IsActive);

        // Activate
        await deviceService.ActivateDeviceAsync(device.Id);
        var activated = await deviceService.GetDeviceByIdAsync(device.Id);
        Assert.NotNull(activated);
        Assert.True(activated.IsActive);
    }

    [Fact]
    public async Task TrackingDeviceService_TenantIsolation_CannotAccessOtherTenantDevice()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (_, _, _, deviceA) = await SeedTelematicsEnvironmentAsync(db, tenantA);

        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var connectivityService = new DeviceConnectivityService(db);
        var deviceServiceB = new TrackingDeviceService(db, userContextB, connectivityService, auditService);

        var result = await deviceServiceB.GetDeviceByIdAsync(deviceA.Id);
        Assert.Null(result);

        var updateReq = new UpdateTrackingDeviceRequest(
            Name: "Unauthorized Update",
            ProviderId: deviceA.ProviderId,
            DeviceTypeId: deviceA.DeviceTypeId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => deviceServiceB.UpdateDeviceAsync(deviceA.Id, updateReq));
    }

    #endregion

    #region 2. Device Assignment Tests

    [Fact]
    public async Task DeviceAssignmentService_AssignDeviceToVehicle_CreatesActiveAssignment()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var assignmentService = new DeviceAssignmentService(db, userContext, auditService);

        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var request = new AssignDeviceToVehicleRequest(
            TrackingDeviceId: device.Id,
            VehicleId: vehicle.Id,
            IsPrimary: true,
            AssignedFromUtc: DateTime.UtcNow,
            Notes: "Assigned for regular route duty");

        var result = await assignmentService.AssignDeviceToVehicleAsync(request);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.TrackingDeviceId);
        Assert.Equal(vehicle.Id, result.VehicleId);
        Assert.True(result.IsActive);
        Assert.True(result.IsPrimary);

        Assert.Single(auditService.WrittenLogs);
        Assert.Equal(AuditAction.TrackingDeviceAssignedToVehicle, auditService.WrittenLogs[0].Action);
    }

    [Fact]
    public async Task DeviceAssignmentService_AssignAlreadyAssignedDevice_ThrowsException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var assignmentService = new DeviceAssignmentService(db, userContext, auditService);

        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        // First assignment
        await assignmentService.AssignDeviceToVehicleAsync(new AssignDeviceToVehicleRequest(
            TrackingDeviceId: device.Id,
            VehicleId: vehicle.Id,
            IsPrimary: true));

        // Attempting second active assignment for the same device
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            assignmentService.AssignDeviceToVehicleAsync(new AssignDeviceToVehicleRequest(
                TrackingDeviceId: device.Id,
                VehicleId: vehicle.Id,
                IsPrimary: true)));
    }

    [Fact]
    public async Task DeviceAssignmentService_AssignCrossTenant_ThrowsException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (vehicleA, _, _, deviceA) = await SeedTelematicsEnvironmentAsync(db, tenantA);
        var (vehicleB, _, _, _) = await SeedTelematicsEnvironmentAsync(db, tenantB);

        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var assignmentServiceA = new DeviceAssignmentService(db, userContextA, auditService);

        // Attempting to assign Tenant A's device to Tenant B's vehicle
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            assignmentServiceA.AssignDeviceToVehicleAsync(new AssignDeviceToVehicleRequest(
                TrackingDeviceId: deviceA.Id,
                VehicleId: vehicleB.Id,
                IsPrimary: true)));
    }

    [Fact]
    public async Task DeviceAssignmentService_UnassignDevice_TerminatesAssignmentAndRetainsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var assignmentService = new DeviceAssignmentService(db, userContext, auditService);

        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var assignment = await assignmentService.AssignDeviceToVehicleAsync(new AssignDeviceToVehicleRequest(
            TrackingDeviceId: device.Id,
            VehicleId: vehicle.Id,
            IsPrimary: true));

        var endResult = await assignmentService.EndDeviceAssignmentAsync(
            assignment.Id,
            new EndDeviceAssignmentRequest(DateTime.UtcNow, "Decommissioning vehicle"));

        Assert.NotNull(endResult);
        Assert.False(endResult.IsActive);

        // Active assignment should now be null
        var active = await assignmentService.GetActiveAssignmentForVehicleAsync(vehicle.Id);
        Assert.Null(active);

        // Historical assignment records should retain history
        var history = await assignmentService.GetAssignmentHistoryForVehicleAsync(vehicle.Id);
        Assert.Single(history);
        Assert.False(history.First().IsActive);
        Assert.NotNull(history.First().AssignedToUtc);
    }

    #endregion

    #region 3. Telemetry Validation & Deduplication Tests

    [Fact]
    public void TelemetryValidationService_ValidMessage_ReturnsSuccess()
    {
        var validationService = new TelemetryValidationService();
        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: "FMC-123456789",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow.AddMinutes(-1),
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060,
            AltitudeMeters: 10,
            SpeedKph: 65.5m,
            HeadingDegrees: 180,
            IgnitionOn: true);

        var result = validationService.Validate(message);
        Assert.True(result.IsValid);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void TelemetryValidationService_InvalidCoordinates_ReturnsFailure()
    {
        var validationService = new TelemetryValidationService();

        var invalidLat = new NormalizedTelemetryMessage(
            DeviceIdentifier: "FMC-123456789",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 95.0, // Invalid: > 90
            Longitude: -74.0060);

        var latResult = validationService.Validate(invalidLat);
        Assert.False(latResult.IsValid);
        Assert.Contains("Latitude", latResult.Reason);

        var invalidLon = new NormalizedTelemetryMessage(
            DeviceIdentifier: "FMC-123456789",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: 195.0); // Invalid: > 180

        var lonResult = validationService.Validate(invalidLon);
        Assert.False(lonResult.IsValid);
        Assert.Contains("Longitude", lonResult.Reason);
    }

    [Fact]
    public void TelemetryValidationService_NegativeSpeed_ReturnsFailure()
    {
        var validationService = new TelemetryValidationService();
        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: "FMC-123456789",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060,
            SpeedKph: -15.0m);

        var result = validationService.Validate(message);
        Assert.False(result.IsValid);
        Assert.Contains("Speed", result.Reason);
    }

    [Fact]
    public void TelemetryValidationService_FutureTimestamp_ReturnsFailure()
    {
        var validationService = new TelemetryValidationService();
        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: "FMC-123456789",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow.AddHours(2), // 2 hours in future (> 15 min tolerance)
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060);

        var result = validationService.Validate(message, maxAcceptedFutureMinutes: 15);
        Assert.False(result.IsValid);
        Assert.Contains("future", result.Reason);
    }

    [Fact]
    public async Task TelemetryDeduplicationService_DuplicateDetection_IdentifiesDuplicate()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var (_, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var recordedAt = DateTime.UtcNow.AddMinutes(-5);
        var existingRecord = new TelemetryRecord(
            tenantId: tenantId,
            trackingDeviceId: device.Id,
            recordedAtUtc: recordedAt,
            receivedAtUtc: DateTime.UtcNow,
            sourceProvider: "Teltonika",
            latitude: 40.7128,
            longitude: -74.0060,
            providerMessageId: "MSG-UNIQUE-101");

        db.TelemetryRecords.Add(existingRecord);
        await db.SaveChangesAsync();

        var dedupService = new TelemetryDeduplicationService(db);

        // 1. Check duplicate by provider message id
        var msgWithSameId = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            ProviderMessageId: "MSG-UNIQUE-101");

        Assert.True(await dedupService.IsDuplicateAsync(tenantId, device.Id, msgWithSameId));

        // 2. Check duplicate by exact timestamp
        var msgWithSameTimestamp = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: recordedAt,
            ReceivedAtUtc: DateTime.UtcNow);

        Assert.True(await dedupService.IsDuplicateAsync(tenantId, device.Id, msgWithSameTimestamp));

        // 3. New message is not duplicate
        var newMessage = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow.AddMinutes(-1),
            ReceivedAtUtc: DateTime.UtcNow,
            ProviderMessageId: "MSG-NEW-202");

        Assert.False(await dedupService.IsDuplicateAsync(tenantId, device.Id, newMessage));
    }

    #endregion

    #region 4. Ingestion & Current State Projection Tests

    [Fact]
    public async Task TelemetryIngestionService_ValidTelemetry_IngestsAndProjectsState()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        // Assign device to vehicle
        var assignment = new TrackingDeviceVehicleAssignment(tenantId, device.Id, vehicle.Id, DateTime.UtcNow, isPrimary: true);
        db.TrackingDeviceVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var validationService = new TelemetryValidationService();
        var dedupService = new TelemetryDeduplicationService(db);
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odometerService = new VehicleOdometerService(db, userContext, auditService);
        var ingestionService = new TelemetryIngestionService(
            db, validationService, dedupService, odometerService, NullLogger<TelemetryIngestionService>.Instance);

        var recordedAt = DateTime.UtcNow.AddSeconds(-30);
        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: recordedAt,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060,
            AltitudeMeters: 25,
            SpeedKph: 55.0m,
            HeadingDegrees: 90,
            IgnitionOn: true,
            OdometerKm: 5010m,
            BatteryVoltage: 12.6m,
            GsmSignal: 4);

        var result = await ingestionService.IngestAsync(message);

        Assert.True(result.Success);
        Assert.NotNull(result.TelemetryRecordId);
        Assert.Equal(device.Id, result.DeviceId);
        Assert.Equal(vehicle.Id, result.VehicleId);
        Assert.False(result.IsDuplicate);

        // Verify TelemetryRecord was persisted
        var persistedRecord = await db.TelemetryRecords.FindAsync(result.TelemetryRecordId);
        Assert.NotNull(persistedRecord);
        Assert.Equal(40.7128, persistedRecord.Latitude);
        Assert.Equal(55.0m, persistedRecord.SpeedKph);

        // Verify Device LastSeen was updated
        var updatedDevice = await db.TrackingDevices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice?.LastSeenAtUtc);
        Assert.Equal(40.7128, updatedDevice.LastKnownLatitude);

        // Verify VehicleTelemetryState projection was created
        var projection = await db.VehicleTelemetryStates.FindAsync(vehicle.Id);
        Assert.NotNull(projection);
        Assert.Equal(40.7128, projection.Latitude);
        Assert.Equal(-74.0060, projection.Longitude);
        Assert.Equal(55.0m, projection.SpeedKph);
        Assert.True(projection.IgnitionOn);
    }

    [Fact]
    public async Task TelemetryIngestionService_OlderPacket_DoesNotOverwriteNewerProjection()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var assignment = new TrackingDeviceVehicleAssignment(tenantId, device.Id, vehicle.Id, DateTime.UtcNow, isPrimary: true);
        db.TrackingDeviceVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var validationService = new TelemetryValidationService();
        var dedupService = new TelemetryDeduplicationService(db);
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odometerService = new VehicleOdometerService(db, userContext, auditService);
        var ingestionService = new TelemetryIngestionService(
            db, validationService, dedupService, odometerService, NullLogger<TelemetryIngestionService>.Instance);

        var now = DateTime.UtcNow;

        // Ingest newer message first (T=now)
        var newerMessage = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: now,
            ReceivedAtUtc: now,
            Latitude: 40.7128,
            Longitude: -74.0060,
            SpeedKph: 60m);
        await ingestionService.IngestAsync(newerMessage);

        // Ingest delayed older message (T=now-10min)
        var olderMessage = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: now.AddMinutes(-10),
            ReceivedAtUtc: now,
            Latitude: 35.0000,
            Longitude: -80.0000,
            SpeedKph: 20m);
        await ingestionService.IngestAsync(olderMessage);

        // The projection must remain at the newer coordinates (40.7128)
        var projection = await db.VehicleTelemetryStates.FindAsync(vehicle.Id);
        Assert.NotNull(projection);
        Assert.Equal(40.7128, projection.Latitude);
        Assert.Equal(60m, projection.SpeedKph);
    }

    [Fact]
    public async Task TelemetryIngestionService_UnregisteredDevice_LogsFailureAndQuarantines()
    {
        using var db = TestDbContextFactory.Create();
        var validationService = new TelemetryValidationService();
        var dedupService = new TelemetryDeduplicationService(db);
        var userContext = new TestUserContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odometerService = new VehicleOdometerService(db, userContext, auditService);
        var ingestionService = new TelemetryIngestionService(
            db, validationService, dedupService, odometerService, NullLogger<TelemetryIngestionService>.Instance);

        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: "UNREGISTERED-DEVICE-999",
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060);

        var result = await ingestionService.IngestAsync(message);

        Assert.False(result.Success);
        Assert.True(result.IsQuarantined);

        // Verify failure was logged
        Assert.Single(db.TelemetryIngestionFailures);
        var failure = db.TelemetryIngestionFailures.First();
        Assert.Equal("UNREGISTERED-DEVICE-999", failure.DeviceIdentifier);
    }

    #endregion

    #region 5. Connectivity & Health Evaluation Tests

    [Fact]
    public void DeviceConnectivityService_EvaluateStatus_ReturnsExpectedStatus()
    {
        using var db = TestDbContextFactory.Create();
        var connectivityService = new DeviceConnectivityService(db);

        // Null timestamp -> Unknown
        Assert.Equal(DeviceConnectivityStatus.Unknown, connectivityService.EvaluateStatus(null));

        // Within threshold -> Online
        var recent = DateTime.UtcNow.AddMinutes(-2);
        Assert.Equal(DeviceConnectivityStatus.Online, connectivityService.EvaluateStatus(recent, offlineThresholdMinutes: 5));

        // Beyond threshold -> Offline
        var old = DateTime.UtcNow.AddMinutes(-10);
        Assert.Equal(DeviceConnectivityStatus.Offline, connectivityService.EvaluateStatus(old, offlineThresholdMinutes: 5));
    }

    [Fact]
    public async Task DeviceHealthService_GetDeviceHealth_CalculatesHealthDto()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        // Update telemetry state on device
        device.UpdateTelemetryState(
            latitude: 40.7128,
            longitude: -74.0060,
            speedKph: 50m,
            headingDegrees: 180,
            ignitionOn: true,
            batteryLevelPercent: 92,
            externalPowerVoltage: 13.8m,
            batteryVoltage: 3.9m,
            signalStrength: 4,
            recordedAtUtc: DateTime.UtcNow.AddMinutes(-1),
            receivedAtUtc: DateTime.UtcNow);

        var assignment = new TrackingDeviceVehicleAssignment(tenantId, device.Id, vehicle.Id, DateTime.UtcNow, isPrimary: true);
        db.TrackingDeviceVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var connectivityService = new DeviceConnectivityService(db);
        var healthService = new DeviceHealthService(db, userContext, connectivityService);

        var health = await healthService.GetDeviceHealthByIdAsync(device.Id);

        Assert.NotNull(health);
        Assert.Equal(device.Id, health.DeviceId);
        Assert.Equal(DeviceConnectivityStatus.Online, health.ConnectivityStatus);
        Assert.Equal(92, health.BatteryLevelPercent);
        Assert.Equal(4, health.SignalStrength);
        Assert.Equal(vehicle.DisplayName, health.VehicleDisplayName);
    }

    #endregion

    #region 6. Teltonika Codec 8 Decoder Tests

    [Fact]
    public void TeltonikaCodec8Parser_ValidPacket_ParsesCorrectly()
    {
        var timestamp = new DateTime(2026, 9, 11, 10, 30, 0, DateTimeKind.Utc);
        var packet = BuildTeltonikaCodec8Packet(
            timestamp: timestamp,
            latitude: 54.6872,
            longitude: 25.2797,
            altitude: 120,
            heading: 270,
            satellites: 14,
            speed: 80,
            ignition: true,
            batteryPct: 95,
            batteryVoltageVolts: 12.6m);

        var messages = TeltonikaCodec8Parser.Parse(packet, "FMC-TEST-DEVICE");

        Assert.NotNull(messages);
        Assert.Single(messages);

        var msg = messages[0];
        Assert.Equal("FMC-TEST-DEVICE", msg.DeviceIdentifier);
        Assert.Equal("Teltonika", msg.Provider);
        Assert.Equal(54.6872, msg.Latitude!.Value, 4);
        Assert.Equal(25.2797, msg.Longitude!.Value, 4);
        Assert.Equal(120, msg.AltitudeMeters);
        Assert.Equal(270, msg.HeadingDegrees);
        Assert.Equal(14, msg.GpsSatellites);
        Assert.Equal(80m, msg.SpeedKph);
        Assert.True(msg.IgnitionOn);
        Assert.Equal(12.6m, msg.BatteryVoltage);
        Assert.Equal(TelemetryEventType.IgnitionOn, msg.EventType);
    }

    [Fact]
    public void TeltonikaCodec8Parser_InvalidPacket_ReturnsEmpty()
    {
        // Empty or too short packet
        var shortPacket = new byte[] { 0x00, 0x01, 0x02 };
        var result = TeltonikaCodec8Parser.Parse(shortPacket);
        Assert.Empty(result);

        // Null packet
        var nullResult = TeltonikaCodec8Parser.Parse(null!);
        Assert.Empty(nullResult);
    }

    #endregion

    #region 7. Odometer Integration Tests

    [Fact]
    public async Task TelemetryIngestionService_HigherOdometer_UpdatesVehicleOdometer()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var (vehicle, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        // Vehicle initial odometer is 5000 km
        Assert.Equal(5000m, vehicle.CurrentOdometer);

        var assignment = new TrackingDeviceVehicleAssignment(tenantId, device.Id, vehicle.Id, DateTime.UtcNow, isPrimary: true);
        db.TrackingDeviceVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var validationService = new TelemetryValidationService();
        var dedupService = new TelemetryDeduplicationService(db);
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odometerService = new VehicleOdometerService(db, userContext, auditService);
        var ingestionService = new TelemetryIngestionService(
            db, validationService, dedupService, odometerService, NullLogger<TelemetryIngestionService>.Instance);

        // Telemetry reports 5025 km (+25 km, threshold is 1.0 km)
        var message = new NormalizedTelemetryMessage(
            DeviceIdentifier: device.DeviceIdentifier,
            Provider: "Teltonika",
            RecordedAtUtc: DateTime.UtcNow,
            ReceivedAtUtc: DateTime.UtcNow,
            Latitude: 40.7128,
            Longitude: -74.0060,
            OdometerKm: 5025m);

        var result = await ingestionService.IngestAsync(message);

        Assert.True(result.Success);

        // Verify vehicle current odometer was updated
        var updatedVehicle = await db.Vehicles.FindAsync(vehicle.Id);
        Assert.NotNull(updatedVehicle);
        Assert.Equal(5025m, updatedVehicle.CurrentOdometer);

        // Verify odometer entry was logged with Telematics source
        var odometerEntry = db.VehicleOdometerEntries.FirstOrDefault(x => x.VehicleId == vehicle.Id && x.Reading == 5025m);
        Assert.NotNull(odometerEntry);
        Assert.Equal(OdometerSource.Telematics, odometerEntry.Source);
    }

    #endregion

    #region 8. Safe Device Commands Tests

    [Fact]
    public async Task DeviceCommandService_SendCommand_CreatesAndMarksSent()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = userId };
        var auditService = new TestAuditLogService();
        var commandService = new DeviceCommandService(db, userContext, auditService);

        var (_, _, _, device) = await SeedTelematicsEnvironmentAsync(db, tenantId);

        var request = new CreateDeviceCommandRequest(
            CommandType: DeviceCommandType.Ping,
            ParametersJson: "{\"timeout\": 30}");

        var command = await commandService.SendCommandAsync(device.Id, request);

        Assert.NotNull(command);
        Assert.Equal(device.Id, command.TrackingDeviceId);
        Assert.Equal(DeviceCommandType.Ping, command.CommandType);
        Assert.Equal(DeviceCommandStatus.Sent, command.Status);
        Assert.NotNull(command.SentAtUtc);

        Assert.Single(auditService.WrittenLogs);
        Assert.Equal(AuditAction.DeviceCommandCreated, auditService.WrittenLogs[0].Action);
    }

    #endregion
}

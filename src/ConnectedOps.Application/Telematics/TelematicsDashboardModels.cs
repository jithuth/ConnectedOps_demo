using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public sealed record TelematicsDashboardDto(
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int FaultedDevices,
    int TrackedVehicles,
    int UntrackedVehicles,
    int VehiclesMoving,
    int VehiclesStopped,
    int VehiclesIgnitionOn,
    int VehiclesIgnitionOff,
    int TelemetryReceivedToday,
    IReadOnlyDictionary<string, int> DevicesByProvider,
    IReadOnlyDictionary<string, int> DevicesByModel,
    IReadOnlyDictionary<string, int> DevicesByHealth,
    IReadOnlyCollection<TrackingDeviceListItemDto> RecentlyOfflineDevices,
    IReadOnlyCollection<TrackingDeviceListItemDto> RecentlySeenDevices);

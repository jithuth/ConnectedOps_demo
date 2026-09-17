using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Predictive;
using Microsoft.AspNetCore.SignalR;

namespace ConnectedOps.Web.Hubs;

public sealed record VehicleTelemetryBroadcast(
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    double Latitude,
    double Longitude,
    decimal SpeedKph,
    decimal Heading,
    decimal OdometerKm,
    DateTime TimestampUtc);

public sealed record EvBatteryBroadcast(
    Guid TenantId,
    Guid VehicleId,
    decimal StateOfChargePercent,
    decimal RemainingRangeKm,
    decimal BatteryPackTempCelsius,
    EvChargingStatus ChargingStatus,
    decimal ActiveChargingPowerKw,
    DateTime TimestampUtc);

public sealed record ChargingSessionBroadcast(
    Guid TenantId,
    Guid SessionId,
    Guid VehicleId,
    Guid ChargingStationId,
    decimal CurrentSocPercent,
    decimal EnergyDeliveredKwh,
    decimal TotalCost,
    bool IsActive);

public sealed record RouteDispatchBroadcast(
    Guid TenantId,
    Guid RunId,
    string RunNumber,
    string Message,
    DateTime TimestampUtc);

public sealed record PredictiveAlertBroadcast(
    Guid TenantId,
    Guid VehicleId,
    string VehicleName,
    string ComponentTitle,
    PredictiveRiskLevel RiskLevel,
    decimal FailureProbability,
    int EstimatedRulDays);

public sealed class FleetHub : Hub
{
    private readonly ILogger<FleetHub> _logger;

    public FleetHub(ILogger<FleetHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTenantGroup(string tenantId)
    {
        if (Guid.TryParse(tenantId, out _))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
            _logger.LogInformation("Connection {ConnectionId} joined tenant group {TenantGroup}", Context.ConnectionId, $"tenant-{tenantId}");
        }
    }

    public async Task LeaveTenantGroup(string tenantId)
    {
        if (Guid.TryParse(tenantId, out _))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
            _logger.LogInformation("Connection {ConnectionId} left tenant group {TenantGroup}", Context.ConnectionId, $"tenant-{tenantId}");
        }
    }

    public async Task SendVehicleTelemetry(VehicleTelemetryBroadcast telemetry)
    {
        await Clients.Group($"tenant-{telemetry.TenantId}").SendAsync("ReceiveVehicleTelemetry", telemetry);
    }

    public async Task SendEvBatteryUpdate(EvBatteryBroadcast battery)
    {
        await Clients.Group($"tenant-{battery.TenantId}").SendAsync("ReceiveEvBatteryTelemetry", battery);
    }

    public async Task SendChargingSessionUpdate(ChargingSessionBroadcast session)
    {
        await Clients.Group($"tenant-{session.TenantId}").SendAsync("ReceiveChargingSessionUpdate", session);
    }

    public async Task SendRouteDispatchUpdate(RouteDispatchBroadcast dispatch)
    {
        await Clients.Group($"tenant-{dispatch.TenantId}").SendAsync("ReceiveRouteDispatchUpdate", dispatch);
    }

    public async Task SendPredictiveAlert(PredictiveAlertBroadcast alert)
    {
        await Clients.Group($"tenant-{alert.TenantId}").SendAsync("ReceivePredictiveAlert", alert);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to FleetHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from FleetHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

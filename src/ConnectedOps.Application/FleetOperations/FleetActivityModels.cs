namespace ConnectedOps.Application.FleetOperations;

public sealed record FleetActivityItemDto(
    Guid Id,
    string EventType,
    string EventTitle,
    DateTime TimestampUtc,
    Guid? VehicleId,
    string? VehicleNumber,
    Guid? DriverId,
    string? DriverName,
    string Description,
    string BadgeClass,
    string IconClass);

public sealed class FleetActivityQueryParameters
{
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? BranchId { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

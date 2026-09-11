using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleDowntimeRecord : BaseEntity
{
    private VehicleDowntimeRecord()
    {
    }

    public VehicleDowntimeRecord(
        Guid tenantId,
        Guid vehicleId,
        DateTime startedAtUtc,
        string reason,
        DowntimeType downtimeType = DowntimeType.Maintenance,
        Guid? maintenanceRecordId = null,
        DateTime? endedAtUtc = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));
        if (endedAtUtc.HasValue && endedAtUtc.Value < startedAtUtc)
            throw new ArgumentException("EndedAtUtc cannot be earlier than StartedAtUtc.");

        TenantId = tenantId;
        VehicleId = vehicleId;
        StartedAtUtc = startedAtUtc;
        Reason = reason.Trim();
        DowntimeType = downtimeType;
        MaintenanceRecordId = maintenanceRecordId;
        EndedAtUtc = endedAtUtc;
        Notes = notes?.Trim();

        if (endedAtUtc.HasValue)
        {
            var span = endedAtUtc.Value - startedAtUtc;
            DurationMinutes = (int)Math.Max(0, span.TotalMinutes);
        }
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid? MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord? MaintenanceRecord { get; private set; }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public int? DurationMinutes { get; private set; }

    public DowntimeType DowntimeType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public void EndDowntime(DateTime endedAtUtc, string? notes = null, Guid? updatedBy = null)
    {
        if (endedAtUtc < StartedAtUtc)
            throw new InvalidOperationException("EndedAtUtc cannot be earlier than StartedAtUtc.");

        EndedAtUtc = endedAtUtc;
        var span = endedAtUtc - StartedAtUtc;
        DurationMinutes = (int)Math.Max(0, span.TotalMinutes);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";
        }
        MarkUpdated(updatedBy);
    }
}

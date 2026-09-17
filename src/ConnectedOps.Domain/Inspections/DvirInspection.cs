using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Inspections;

public sealed class DvirInspection : BaseEntity
{
    private readonly List<DvirItemCheck> _items = [];

    private DvirInspection()
    {
    }

    public DvirInspection(
        Guid tenantId,
        string inspectionNumber,
        Guid vehicleId,
        Guid driverId,
        DvirType inspectionType,
        decimal odometer,
        string? locationName = null,
        string? driverSignatureData = null,
        string? remarks = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(inspectionNumber))
            throw new ArgumentException("InspectionNumber is required.", nameof(inspectionNumber));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        TenantId = tenantId;
        InspectionNumber = inspectionNumber.Trim().ToUpperInvariant();
        VehicleId = vehicleId;
        DriverId = driverId;
        InspectionType = inspectionType;
        Odometer = odometer >= 0 ? odometer : 0m;
        LocationName = locationName?.Trim();
        DriverSignatureData = driverSignatureData;
        Remarks = remarks?.Trim();
        InspectedAtUtc = DateTime.UtcNow;
        Status = DvirStatus.Passed;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string InspectionNumber { get; private set; } = string.Empty;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public DvirType InspectionType { get; private set; }
    public DvirStatus Status { get; private set; }
    public decimal Odometer { get; private set; }
    public string? LocationName { get; private set; }
    public DateTime InspectedAtUtc { get; private set; }

    public string? DriverSignatureData { get; private set; }
    public string? Remarks { get; private set; }

    public bool HasDefects => _items.Any(i => !i.IsPassed);
    public bool HasCriticalDefect => _items.Any(i => !i.IsPassed && i.Severity == DefectSeverity.Critical);

    public string? MechanicName { get; private set; }
    public string? MechanicNotes { get; private set; }
    public string? MechanicSignatureData { get; private set; }
    public DateTime? CertifiedAtUtc { get; private set; }
    public Guid? CertifiedByUserId { get; private set; }

    public IReadOnlyCollection<DvirItemCheck> Items => _items.AsReadOnly();

    public void AddItemCheck(DvirItemCheck item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);

        if (!item.IsPassed)
        {
            if (item.Severity == DefectSeverity.Critical)
            {
                Status = DvirStatus.VehicleGrounded;
            }
            else if (Status != DvirStatus.VehicleGrounded)
            {
                Status = DvirStatus.DefectsReported;
            }
        }
    }

    public void CertifySafeByMechanic(
        string mechanicName,
        string? notes,
        string? signatureData,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(mechanicName))
            throw new ArgumentException("MechanicName is required.", nameof(mechanicName));

        MechanicName = mechanicName.Trim();
        MechanicNotes = notes?.Trim();
        MechanicSignatureData = signatureData;
        CertifiedAtUtc = DateTime.UtcNow;
        CertifiedByUserId = userId;
        Status = DvirStatus.CertifiedSafe;
        MarkUpdated(userId);
    }
}

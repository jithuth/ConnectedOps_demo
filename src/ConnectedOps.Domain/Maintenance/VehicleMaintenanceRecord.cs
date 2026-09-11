using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceRecord : BaseEntity
{
    private readonly List<VehicleMaintenanceTask> _tasks = [];
    private readonly List<VehicleMaintenancePart> _parts = [];
    private readonly List<VehicleMaintenanceLabour> _labour = [];
    private readonly List<VehicleMaintenanceExpense> _expenses = [];
    private readonly List<VehicleMaintenanceDocument> _documents = [];
    private readonly List<VehicleDowntimeRecord> _downtimeRecords = [];

    private VehicleMaintenanceRecord()
    {
    }

    public VehicleMaintenanceRecord(
        Guid tenantId,
        Guid vehicleId,
        Guid maintenanceServiceTypeId,
        DateTime serviceDateUtc,
        Guid? maintenancePlanId = null,
        Guid? maintenancePlanRuleId = null,
        Guid? maintenanceProviderId = null,
        decimal? odometerReading = null,
        OdometerUnit odometerUnit = OdometerUnit.Kilometers,
        decimal? engineHours = null,
        VehicleMaintenanceType maintenanceType = VehicleMaintenanceType.Scheduled,
        MaintenanceRecordStatus status = MaintenanceRecordStatus.Draft,
        string? referenceNumber = null,
        string? description = null,
        string? technicianNotes = null,
        string? currencyCode = "USD",
        DateTime? startDateTimeUtc = null,
        DateTime? completedDateTimeUtc = null,
        int? vehicleDowntimeMinutes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (maintenanceServiceTypeId == Guid.Empty)
            throw new ArgumentException("MaintenanceServiceTypeId is required.", nameof(maintenanceServiceTypeId));
        if (odometerReading.HasValue && odometerReading.Value < 0)
            throw new ArgumentException("Odometer reading cannot be negative.", nameof(odometerReading));
        if (engineHours.HasValue && engineHours.Value < 0)
            throw new ArgumentException("Engine hours cannot be negative.", nameof(engineHours));

        TenantId = tenantId;
        VehicleId = vehicleId;
        MaintenanceServiceTypeId = maintenanceServiceTypeId;
        ServiceDateUtc = serviceDateUtc;
        MaintenancePlanId = maintenancePlanId;
        MaintenancePlanRuleId = maintenancePlanRuleId;
        MaintenanceProviderId = maintenanceProviderId;
        OdometerReading = odometerReading;
        OdometerUnit = odometerUnit;
        EngineHours = engineHours;
        MaintenanceType = maintenanceType;
        Status = status;
        ReferenceNumber = referenceNumber?.Trim();
        Description = description?.Trim();
        TechnicianNotes = technicianNotes?.Trim();
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        StartDateTimeUtc = startDateTimeUtc;
        CompletedDateTimeUtc = completedDateTimeUtc;
        VehicleDowntimeMinutes = vehicleDowntimeMinutes;
        CreatedByUserId = createdByUserId;
        CreatedBy = createdByUserId;

        TotalPartsCost = 0m;
        TotalLabourCost = 0m;
        OtherCost = 0m;
        TotalCost = 0m;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid? MaintenancePlanId { get; private set; }
    public MaintenancePlan? MaintenancePlan { get; private set; }

    public Guid? MaintenancePlanRuleId { get; private set; }
    public MaintenancePlanRule? MaintenancePlanRule { get; private set; }

    public Guid MaintenanceServiceTypeId { get; private set; }
    public MaintenanceServiceType MaintenanceServiceType { get; private set; } = null!;

    public Guid? MaintenanceProviderId { get; private set; }
    public MaintenanceProvider? MaintenanceProvider { get; private set; }

    public DateTime ServiceDateUtc { get; private set; }
    public DateTime? StartDateTimeUtc { get; private set; }
    public DateTime? CompletedDateTimeUtc { get; private set; }

    public decimal? OdometerReading { get; private set; }
    public OdometerUnit OdometerUnit { get; private set; }
    public decimal? EngineHours { get; private set; }

    public VehicleMaintenanceType MaintenanceType { get; private set; }
    public MaintenanceRecordStatus Status { get; private set; }

    public string? ReferenceNumber { get; private set; }
    public string? Description { get; private set; }
    public string? TechnicianNotes { get; private set; }

    public decimal? TotalPartsCost { get; private set; }
    public decimal? TotalLabourCost { get; private set; }
    public decimal? OtherCost { get; private set; }
    public decimal? TotalCost { get; private set; }

    public string? CurrencyCode { get; private set; }
    public int? VehicleDowntimeMinutes { get; private set; }

    public Guid? CreatedByUserId { get; private set; }
    public Guid? CompletedByUserId { get; private set; }

    public IReadOnlyCollection<VehicleMaintenanceTask> Tasks => _tasks;
    public IReadOnlyCollection<VehicleMaintenancePart> Parts => _parts;
    public IReadOnlyCollection<VehicleMaintenanceLabour> Labour => _labour;
    public IReadOnlyCollection<VehicleMaintenanceExpense> Expenses => _expenses;
    public IReadOnlyCollection<VehicleMaintenanceDocument> Documents => _documents;
    public IReadOnlyCollection<VehicleDowntimeRecord> DowntimeRecords => _downtimeRecords;

    public void UpdateGeneral(
        Guid maintenanceServiceTypeId,
        DateTime serviceDateUtc,
        Guid? maintenancePlanId,
        Guid? maintenancePlanRuleId,
        Guid? maintenanceProviderId,
        VehicleMaintenanceType maintenanceType,
        decimal? odometerReading,
        OdometerUnit odometerUnit,
        decimal? engineHours,
        string? referenceNumber,
        string? description,
        string? technicianNotes,
        string? currencyCode,
        Guid? updatedBy = null)
    {
        if (Status == MaintenanceRecordStatus.Completed)
            throw new InvalidOperationException("Completed maintenance records cannot be edited.");
        if (Status == MaintenanceRecordStatus.Cancelled)
            throw new InvalidOperationException("Cancelled maintenance records cannot be edited.");

        MaintenanceServiceTypeId = maintenanceServiceTypeId;
        ServiceDateUtc = serviceDateUtc;
        MaintenancePlanId = maintenancePlanId;
        MaintenancePlanRuleId = maintenancePlanRuleId;
        MaintenanceProviderId = maintenanceProviderId;
        MaintenanceType = maintenanceType;
        OdometerReading = odometerReading;
        OdometerUnit = odometerUnit;
        EngineHours = engineHours;
        ReferenceNumber = referenceNumber?.Trim();
        Description = description?.Trim();
        TechnicianNotes = technicianNotes?.Trim();
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        MarkUpdated(updatedBy);
    }

    public void StartService(DateTime? startDateTimeUtc = null, Guid? updatedBy = null)
    {
        if (Status == MaintenanceRecordStatus.Completed)
            throw new InvalidOperationException("Cannot start an already completed service record.");
        if (Status == MaintenanceRecordStatus.Cancelled)
            throw new InvalidOperationException("Cannot start a cancelled service record.");

        Status = MaintenanceRecordStatus.InProgress;
        StartDateTimeUtc = startDateTimeUtc ?? DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void CompleteService(
        DateTime completedDateTimeUtc,
        decimal? finalOdometer,
        decimal? finalEngineHours,
        Guid? completedByUserId,
        string? notes = null)
    {
        if (Status == MaintenanceRecordStatus.Completed)
            throw new InvalidOperationException("This maintenance record is already completed.");
        if (Status == MaintenanceRecordStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled service record.");

        if (StartDateTimeUtc.HasValue && completedDateTimeUtc < StartDateTimeUtc.Value)
        {
            throw new InvalidOperationException("Completed date cannot be earlier than start date.");
        }

        Status = MaintenanceRecordStatus.Completed;
        CompletedDateTimeUtc = completedDateTimeUtc;
        CompletedByUserId = completedByUserId;
        if (finalOdometer.HasValue)
        {
            OdometerReading = finalOdometer.Value;
        }
        if (finalEngineHours.HasValue)
        {
            EngineHours = finalEngineHours.Value;
        }
        if (!string.IsNullOrWhiteSpace(notes))
        {
            TechnicianNotes = string.IsNullOrWhiteSpace(TechnicianNotes) ? notes.Trim() : $"{TechnicianNotes}\n{notes.Trim()}";
        }

        if (StartDateTimeUtc.HasValue)
        {
            var span = completedDateTimeUtc - StartDateTimeUtc.Value;
            VehicleDowntimeMinutes = (int)Math.Max(0, span.TotalMinutes);
        }

        RecalculateTotals();
        MarkUpdated(completedByUserId);
    }

    public void CancelService(string? reason = null, Guid? cancelledByUserId = null)
    {
        if (Status == MaintenanceRecordStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed service record.");

        Status = MaintenanceRecordStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            TechnicianNotes = string.IsNullOrWhiteSpace(TechnicianNotes) ? $"Cancelled: {reason.Trim()}" : $"{TechnicianNotes}\nCancelled: {reason.Trim()}";
        }
        MarkUpdated(cancelledByUserId);
    }

    public void RecalculateTotals()
    {
        var partsTotal = _parts.Sum(p => p.TotalCost ?? (p.Quantity * (p.UnitCost ?? 0m)));
        var labourTotal = _labour.Sum(l => l.TotalCost ?? (l.Hours * (l.HourlyRate ?? 0m)));
        var expenseTotal = _expenses.Sum(e => e.Amount);

        TotalPartsCost = partsTotal;
        TotalLabourCost = labourTotal;
        OtherCost = expenseTotal;
        TotalCost = partsTotal + labourTotal + expenseTotal;
    }
}

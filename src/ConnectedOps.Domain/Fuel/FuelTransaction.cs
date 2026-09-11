using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelTransaction : BaseEntity
{
    private readonly List<FuelTransactionDocument> _documents = [];
    private readonly List<FuelAnomaly> _anomalies = [];

    private FuelTransaction()
    {
    }

    public FuelTransaction(
        Guid tenantId,
        Guid vehicleId,
        DateTime transactionDateUtc,
        decimal quantity,
        decimal unitPrice,
        decimal totalCost,
        FuelType fuelType = FuelType.Diesel,
        FuelUnit quantityUnit = FuelUnit.Liter,
        string? currencyCode = "USD",
        bool isFullTank = true,
        bool isPartialFill = false,
        decimal? odometerReading = null,
        OdometerUnit? odometerUnit = null,
        decimal? engineHours = null,
        Guid? driverId = null,
        Guid? fuelStationId = null,
        Guid? fuelCardId = null,
        FuelTransactionSource source = FuelTransactionSource.Manual,
        FuelTransactionStatus status = FuelTransactionStatus.Confirmed,
        string? transactionReference = null,
        string? receiptNumber = null,
        string? paymentMethod = null,
        string? externalTransactionId = null,
        double? latitude = null,
        double? longitude = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (totalCost < 0)
            throw new ArgumentException("Total cost cannot be negative.", nameof(totalCost));
        if (odometerReading.HasValue && odometerReading.Value < 0)
            throw new ArgumentException("Odometer reading cannot be negative.", nameof(odometerReading));
        if (engineHours.HasValue && engineHours.Value < 0)
            throw new ArgumentException("Engine hours cannot be negative.", nameof(engineHours));
        if (latitude.HasValue && (latitude.Value < -90.0 || latitude.Value > 90.0))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees.", nameof(latitude));
        if (longitude.HasValue && (longitude.Value < -180.0 || longitude.Value > 180.0))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees.", nameof(longitude));

        TenantId = tenantId;
        VehicleId = vehicleId;
        TransactionDateUtc = transactionDateUtc;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalCost = totalCost;
        FuelType = fuelType;
        QuantityUnit = quantityUnit;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        IsFullTank = isFullTank;
        IsPartialFill = isPartialFill;
        OdometerReading = odometerReading;
        OdometerUnit = odometerUnit;
        EngineHours = engineHours;
        DriverId = driverId;
        FuelStationId = fuelStationId;
        FuelCardId = fuelCardId;
        Source = source;
        Status = status;
        TransactionReference = transactionReference?.Trim();
        ReceiptNumber = receiptNumber?.Trim();
        PaymentMethod = paymentMethod?.Trim();
        ExternalTransactionId = externalTransactionId?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Notes = notes?.Trim();
        CreatedByUserId = createdByUserId;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? FuelStationId { get; private set; }
    public FuelStation? FuelStation { get; private set; }

    public Guid? FuelCardId { get; private set; }
    public FuelCard? FuelCard { get; private set; }

    public FuelType FuelType { get; private set; }
    public DateTime TransactionDateUtc { get; private set; }

    public decimal? OdometerReading { get; private set; }
    public OdometerUnit? OdometerUnit { get; private set; }
    public decimal? EngineHours { get; private set; }

    public decimal Quantity { get; private set; }
    public FuelUnit QuantityUnit { get; private set; }

    public decimal UnitPrice { get; private set; }
    public decimal TotalCost { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";

    public bool IsFullTank { get; private set; }
    public bool IsPartialFill { get; private set; }

    public FuelTransactionStatus Status { get; private set; }
    public FuelTransactionSource Source { get; private set; }

    public string? TransactionReference { get; private set; }
    public string? ReceiptNumber { get; private set; }
    public string? PaymentMethod { get; private set; }
    public string? ExternalTransactionId { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public string? Notes { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public IReadOnlyCollection<FuelTransactionDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<FuelAnomaly> Anomalies => _anomalies.AsReadOnly();

    public void Update(
        Guid vehicleId,
        DateTime transactionDateUtc,
        decimal quantity,
        decimal unitPrice,
        decimal totalCost,
        FuelType fuelType,
        FuelUnit quantityUnit,
        string? currencyCode,
        bool isFullTank,
        bool isPartialFill,
        decimal? odometerReading,
        OdometerUnit? odometerUnit,
        decimal? engineHours,
        Guid? driverId,
        Guid? fuelStationId,
        Guid? fuelCardId,
        string? transactionReference,
        string? receiptNumber,
        string? paymentMethod,
        double? latitude,
        double? longitude,
        string? notes,
        Guid? updatedBy = null)
    {
        if (Status == FuelTransactionStatus.Cancelled)
            throw new InvalidOperationException("Cannot modify a cancelled fuel transaction.");
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (totalCost < 0)
            throw new ArgumentException("Total cost cannot be negative.", nameof(totalCost));
        if (odometerReading.HasValue && odometerReading.Value < 0)
            throw new ArgumentException("Odometer reading cannot be negative.", nameof(odometerReading));
        if (engineHours.HasValue && engineHours.Value < 0)
            throw new ArgumentException("Engine hours cannot be negative.", nameof(engineHours));
        if (latitude.HasValue && (latitude.Value < -90.0 || latitude.Value > 90.0))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees.", nameof(latitude));
        if (longitude.HasValue && (longitude.Value < -180.0 || longitude.Value > 180.0))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees.", nameof(longitude));

        VehicleId = vehicleId;
        TransactionDateUtc = transactionDateUtc;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalCost = totalCost;
        FuelType = fuelType;
        QuantityUnit = quantityUnit;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        IsFullTank = isFullTank;
        IsPartialFill = isPartialFill;
        OdometerReading = odometerReading;
        OdometerUnit = odometerUnit;
        EngineHours = engineHours;
        DriverId = driverId;
        FuelStationId = fuelStationId;
        FuelCardId = fuelCardId;
        TransactionReference = transactionReference?.Trim();
        ReceiptNumber = receiptNumber?.Trim();
        PaymentMethod = paymentMethod?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }

    public void Cancel(string? cancellationReason = null, Guid? cancelledBy = null)
    {
        if (Status == FuelTransactionStatus.Cancelled)
            return;

        Status = FuelTransactionStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(cancellationReason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? $"[Cancelled: {cancellationReason.Trim()}]"
                : $"{Notes}\n[Cancelled: {cancellationReason.Trim()}]";
        }
        MarkUpdated(cancelledBy);
    }
}

using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Maintenance;

public sealed record VehicleMaintenanceRecordListItemDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid MaintenanceServiceTypeId,
    string ServiceTypeCode,
    string ServiceTypeName,
    MaintenanceServiceCategory ServiceCategory,
    string ServiceCategoryName,
    Guid? MaintenancePlanId,
    string? PlanName,
    Guid? MaintenanceProviderId,
    string? ProviderName,
    DateTime ServiceDateUtc,
    DateTime? StartDateTimeUtc,
    DateTime? CompletedDateTimeUtc,
    decimal? OdometerReading,
    OdometerUnit OdometerUnit,
    decimal? EngineHours,
    VehicleMaintenanceType MaintenanceType,
    string MaintenanceTypeName,
    MaintenanceRecordStatus Status,
    string StatusName,
    string? ReferenceNumber,
    decimal TotalCost,
    string CurrencyCode,
    int? VehicleDowntimeMinutes,
    int TaskCount,
    int PartCount,
    int DocumentCount,
    DateTime CreatedAtUtc);

public sealed record VehicleMaintenanceRecordDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid MaintenanceServiceTypeId,
    string ServiceTypeCode,
    string ServiceTypeName,
    MaintenanceServiceCategory ServiceCategory,
    string ServiceCategoryName,
    Guid? MaintenancePlanId,
    string? PlanName,
    Guid? MaintenancePlanRuleId,
    Guid? MaintenanceProviderId,
    string? ProviderName,
    DateTime ServiceDateUtc,
    DateTime? StartDateTimeUtc,
    DateTime? CompletedDateTimeUtc,
    decimal? OdometerReading,
    OdometerUnit OdometerUnit,
    decimal? EngineHours,
    VehicleMaintenanceType MaintenanceType,
    string MaintenanceTypeName,
    MaintenanceRecordStatus Status,
    string StatusName,
    string? ReferenceNumber,
    string? Description,
    string? TechnicianNotes,
    decimal? TotalPartsCost,
    decimal? TotalLabourCost,
    decimal? OtherCost,
    decimal TotalCost,
    string CurrencyCode,
    int? VehicleDowntimeMinutes,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    Guid? CompletedByUserId,
    string? CompletedByUserName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<VehicleMaintenanceTaskDto> Tasks,
    IReadOnlyCollection<VehicleMaintenancePartDto> Parts,
    IReadOnlyCollection<VehicleMaintenanceLabourDto> Labour,
    IReadOnlyCollection<VehicleMaintenanceExpenseDto> Expenses,
    IReadOnlyCollection<VehicleMaintenanceDocumentDto> Documents,
    IReadOnlyCollection<VehicleDowntimeRecordDto> DowntimeRecords);

public sealed record VehicleMaintenanceTaskDto(
    Guid Id,
    Guid MaintenanceRecordId,
    Guid? MaintenanceServiceTypeId,
    string? ServiceTypeName,
    string Name,
    string? Description,
    MaintenanceTaskStatus Status,
    string StatusName,
    DateTime? CompletedAtUtc,
    Guid? CompletedByUserId,
    string? CompletedByUserName,
    string? Notes);

public sealed record VehicleMaintenancePartDto(
    Guid Id,
    Guid MaintenanceRecordId,
    string? PartNumber,
    string PartName,
    decimal Quantity,
    string Unit,
    decimal? UnitCost,
    decimal? TotalCost,
    string? Supplier,
    string? Notes);

public sealed record VehicleMaintenanceLabourDto(
    Guid Id,
    Guid MaintenanceRecordId,
    string Description,
    decimal Hours,
    decimal? HourlyRate,
    decimal? TotalCost,
    string? TechnicianName,
    Guid? EmployeeId,
    string? EmployeeName,
    string? Notes);

public sealed record VehicleMaintenanceExpenseDto(
    Guid Id,
    Guid MaintenanceRecordId,
    MaintenanceExpenseType ExpenseType,
    string ExpenseTypeName,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string? Reference,
    string? Notes);

public sealed record VehicleDowntimeRecordDto(
    Guid Id,
    Guid VehicleId,
    Guid? MaintenanceRecordId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int? DurationMinutes,
    DowntimeType DowntimeType,
    string DowntimeTypeName,
    string Reason,
    string? Notes);

public sealed record VehicleMaintenanceDocumentDto(
    Guid Id,
    Guid MaintenanceRecordId,
    MaintenanceDocumentType DocumentType,
    string DocumentTypeName,
    string Title,
    string FileObjectKey,
    string FileName,
    string? ContentType,
    long? FileSizeBytes,
    DateTime UploadedAtUtc,
    Guid? UploadedByUserId,
    string? UploadedByUserName,
    string? Notes);

public sealed record CreateMaintenanceRecordRequest(
    Guid VehicleId,
    Guid MaintenanceServiceTypeId,
    DateTime ServiceDateUtc,
    Guid? MaintenancePlanId = null,
    Guid? MaintenancePlanRuleId = null,
    Guid? MaintenanceProviderId = null,
    decimal? OdometerReading = null,
    OdometerUnit OdometerUnit = OdometerUnit.Kilometers,
    decimal? EngineHours = null,
    VehicleMaintenanceType MaintenanceType = VehicleMaintenanceType.Scheduled,
    MaintenanceRecordStatus InitialStatus = MaintenanceRecordStatus.Draft,
    string? ReferenceNumber = null,
    string? Description = null,
    string? TechnicianNotes = null,
    string? CurrencyCode = "USD",
    IReadOnlyCollection<AddMaintenanceTaskRequest>? InitialTasks = null);

public sealed record UpdateMaintenanceRecordRequest(
    Guid MaintenanceServiceTypeId,
    DateTime ServiceDateUtc,
    Guid? MaintenancePlanId,
    Guid? MaintenancePlanRuleId,
    Guid? MaintenanceProviderId,
    VehicleMaintenanceType MaintenanceType,
    decimal? OdometerReading,
    OdometerUnit OdometerUnit,
    decimal? EngineHours,
    string? ReferenceNumber,
    string? Description,
    string? TechnicianNotes,
    string? CurrencyCode = "USD");

public sealed record StartMaintenanceRecordRequest(
    DateTime? StartDateTimeUtc = null);

public sealed record CompleteMaintenanceRecordRequest(
    DateTime CompletedDateTimeUtc,
    decimal? FinalOdometer = null,
    decimal? FinalEngineHours = null,
    string? Notes = null);

public sealed record CancelMaintenanceRecordRequest(
    string? Reason = null);

public sealed record AddMaintenanceTaskRequest(
    string Name,
    Guid? MaintenanceServiceTypeId = null,
    string? Description = null,
    string? Notes = null);

public sealed record UpdateMaintenanceTaskStatusRequest(
    MaintenanceTaskStatus Status,
    string? Notes = null);

public sealed record AddMaintenancePartRequest(
    string PartName,
    decimal Quantity,
    string? PartNumber = null,
    string Unit = "PCS",
    decimal? UnitCost = null,
    string? Supplier = null,
    string? Notes = null);

public sealed record AddMaintenanceLabourRequest(
    string Description,
    decimal Hours,
    decimal? HourlyRate = null,
    string? TechnicianName = null,
    Guid? EmployeeId = null,
    string? Notes = null);

public sealed record AddMaintenanceExpenseRequest(
    MaintenanceExpenseType ExpenseType,
    string Description,
    decimal Amount,
    string CurrencyCode = "USD",
    string? Reference = null,
    string? Notes = null);

public sealed record AddMaintenanceDocumentRequest(
    MaintenanceDocumentType DocumentType,
    string Title,
    string FileObjectKey,
    string FileName,
    string? ContentType = null,
    long? FileSizeBytes = null,
    string? Notes = null);

public sealed record MaintenanceRecordQueryParameters
{
    public Guid? VehicleId { get; init; }
    public Guid? ServiceTypeId { get; init; }
    public Guid? ProviderId { get; init; }
    public Guid? MaintenancePlanId { get; init; }
    public VehicleMaintenanceType? MaintenanceType { get; init; }
    public MaintenanceRecordStatus? Status { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

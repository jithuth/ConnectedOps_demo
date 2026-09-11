using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IVehicleService _vehicleService;
    private readonly IMaintenanceDueEvaluationService _dueEvaluationService;

    public MaintenanceRecordService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IVehicleOdometerService odometerService,
        IVehicleService vehicleService,
        IMaintenanceDueEvaluationService dueEvaluationService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _odometerService = odometerService;
        _vehicleService = vehicleService;
        _dueEvaluationService = dueEvaluationService;
    }

    public async Task<PagedResult<VehicleMaintenanceRecordListItemDto>> GetRecordsAsync(
        MaintenanceRecordQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var dbQuery = _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenanceServiceType)
            .Include(x => x.MaintenancePlan)
            .Include(x => x.MaintenanceProvider)
            .Include(x => x.Tasks)
            .Include(x => x.Parts)
            .Include(x => x.Documents)
            .Where(x => x.TenantId == tenantId);

        if (query.VehicleId.HasValue)
            dbQuery = dbQuery.Where(x => x.VehicleId == query.VehicleId.Value);

        if (query.ServiceTypeId.HasValue)
            dbQuery = dbQuery.Where(x => x.MaintenanceServiceTypeId == query.ServiceTypeId.Value);

        if (query.ProviderId.HasValue)
            dbQuery = dbQuery.Where(x => x.MaintenanceProviderId == query.ProviderId.Value);

        if (query.MaintenancePlanId.HasValue)
            dbQuery = dbQuery.Where(x => x.MaintenancePlanId == query.MaintenancePlanId.Value);

        if (query.MaintenanceType.HasValue)
            dbQuery = dbQuery.Where(x => x.MaintenanceType == query.MaintenanceType.Value);

        if (query.Status.HasValue)
            dbQuery = dbQuery.Where(x => x.Status == query.Status.Value);

        if (query.FromUtc.HasValue)
            dbQuery = dbQuery.Where(x => x.ServiceDateUtc >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            dbQuery = dbQuery.Where(x => x.ServiceDateUtc <= query.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(x =>
                x.Vehicle.VehicleNumber.ToLowerInvariant().Contains(search) ||
                (x.Vehicle.RegistrationNumber != null && x.Vehicle.RegistrationNumber.ToLowerInvariant().Contains(search)) ||
                (x.ReferenceNumber != null && x.ReferenceNumber.ToLowerInvariant().Contains(search)) ||
                x.MaintenanceServiceType.Name.ToLowerInvariant().Contains(search));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var page = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(x => x.ServiceDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToListItemDto).ToList();
        return new PagedResult<VehicleMaintenanceRecordListItemDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<VehicleMaintenanceRecordDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenanceServiceType)
            .Include(x => x.MaintenancePlan)
            .Include(x => x.MaintenancePlanRule)
            .Include(x => x.MaintenanceProvider)
            .Include(x => x.Tasks.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.MaintenanceServiceType)
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Employee)
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .Include(x => x.Documents.Where(d => !d.IsDeleted))
            .Include(x => x.DowntimeRecords.Where(dt => !dt.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{id}' was not found.");

        return MapToDetailDto(record);
    }

    public async Task<IReadOnlyCollection<VehicleMaintenanceRecordListItemDto>> GetVehicleMaintenanceHistoryAsync(
        Guid vehicleId,
        MaintenanceRecordQueryParameters? query = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var dbQuery = _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenanceServiceType)
            .Include(x => x.MaintenancePlan)
            .Include(x => x.MaintenanceProvider)
            .Include(x => x.Tasks)
            .Include(x => x.Parts)
            .Include(x => x.Documents)
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId);

        if (query?.ServiceTypeId.HasValue == true)
            dbQuery = dbQuery.Where(x => x.MaintenanceServiceTypeId == query.ServiceTypeId.Value);

        if (query?.MaintenanceType.HasValue == true)
            dbQuery = dbQuery.Where(x => x.MaintenanceType == query.MaintenanceType.Value);

        if (query?.Status.HasValue == true)
            dbQuery = dbQuery.Where(x => x.Status == query.Status.Value);

        if (query?.FromUtc.HasValue == true)
            dbQuery = dbQuery.Where(x => x.ServiceDateUtc >= query.FromUtc.Value);

        if (query?.ToUtc.HasValue == true)
            dbQuery = dbQuery.Where(x => x.ServiceDateUtc <= query.ToUtc.Value);

        var list = await dbQuery
            .OrderByDescending(x => x.ServiceDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToListItemDto).ToList();
    }

    public async Task<VehicleMaintenanceRecordDto> CreateAsync(
        CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(x => x.Id == request.MaintenanceServiceTypeId && x.TenantId == tenantId, cancellationToken);

        if (serviceType is null)
            throw new KeyNotFoundException($"Maintenance service type '{request.MaintenanceServiceTypeId}' was not found.");

        if (request.MaintenanceProviderId.HasValue)
        {
            var providerExists = await _dbContext.MaintenanceProviders
                .AnyAsync(x => x.Id == request.MaintenanceProviderId.Value && x.TenantId == tenantId, cancellationToken);

            if (!providerExists)
                throw new KeyNotFoundException($"Maintenance provider '{request.MaintenanceProviderId.Value}' was not found.");
        }

        if (request.MaintenancePlanId.HasValue)
        {
            var planExists = await _dbContext.MaintenancePlans
                .AnyAsync(x => x.Id == request.MaintenancePlanId.Value && x.TenantId == tenantId, cancellationToken);

            if (!planExists)
                throw new KeyNotFoundException($"Maintenance plan '{request.MaintenancePlanId.Value}' was not found.");
        }

        var recordOdometer = request.OdometerReading ?? vehicle.CurrentOdometer;

        var record = new VehicleMaintenanceRecord(
            tenantId,
            request.VehicleId,
            request.MaintenanceServiceTypeId,
            request.ServiceDateUtc,
            request.MaintenancePlanId,
            request.MaintenancePlanRuleId,
            request.MaintenanceProviderId,
            recordOdometer,
            request.OdometerUnit,
            request.EngineHours,
            request.MaintenanceType,
            request.InitialStatus,
            request.ReferenceNumber,
            request.Description,
            request.TechnicianNotes,
            request.CurrencyCode ?? vehicle.CurrencyCode,
            createdByUserId: _currentUserContext.UserId);

        _dbContext.VehicleMaintenanceRecords.Add(record);

        if (request.InitialTasks is not null && request.InitialTasks.Count > 0)
        {
            foreach (var taskReq in request.InitialTasks)
            {
                var task = new VehicleMaintenanceTask(
                    tenantId,
                    record.Id,
                    taskReq.Name,
                    taskReq.MaintenanceServiceTypeId,
                    taskReq.Description,
                    MaintenanceTaskStatus.Pending,
                    taskReq.Notes);

                _dbContext.VehicleMaintenanceTasks.Add(task);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceRecordCreated,
                "VehicleMaintenanceRecord",
                record.Id.ToString(),
                $"Created maintenance record for vehicle {vehicle.VehicleNumber} ({serviceType.Name})"),
            cancellationToken);

        return await GetByIdAsync(record.Id, cancellationToken);
    }

    public async Task<VehicleMaintenanceRecordDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{id}' was not found.");

        if (record.Status is MaintenanceRecordStatus.Completed or MaintenanceRecordStatus.Cancelled)
            throw new InvalidOperationException($"Cannot update maintenance record in status '{record.Status}'.");

        var serviceTypeExists = await _dbContext.MaintenanceServiceTypes
            .AnyAsync(x => x.Id == request.MaintenanceServiceTypeId && x.TenantId == tenantId, cancellationToken);

        if (!serviceTypeExists)
            throw new KeyNotFoundException($"Maintenance service type '{request.MaintenanceServiceTypeId}' was not found.");

        if (request.MaintenanceProviderId.HasValue)
        {
            var providerExists = await _dbContext.MaintenanceProviders
                .AnyAsync(x => x.Id == request.MaintenanceProviderId.Value && x.TenantId == tenantId, cancellationToken);

            if (!providerExists)
                throw new KeyNotFoundException($"Maintenance provider '{request.MaintenanceProviderId.Value}' was not found.");
        }

        record.UpdateGeneral(
            request.MaintenanceServiceTypeId,
            request.ServiceDateUtc,
            request.MaintenancePlanId,
            request.MaintenancePlanRuleId,
            request.MaintenanceProviderId,
            request.MaintenanceType,
            request.OdometerReading,
            request.OdometerUnit,
            request.EngineHours,
            request.ReferenceNumber,
            request.Description,
            request.TechnicianNotes,
            request.CurrencyCode,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceRecordUpdated,
                "VehicleMaintenanceRecord",
                record.Id.ToString(),
                $"Updated maintenance record {record.Id}"),
            cancellationToken);

        return await GetByIdAsync(record.Id, cancellationToken);
    }

    public async Task<VehicleMaintenanceRecordDto> StartServiceAsync(
        Guid id,
        StartMaintenanceRecordRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenanceServiceType)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{id}' was not found.");

        var startTime = request?.StartDateTimeUtc ?? DateTime.UtcNow;
        record.StartService(startTime, _currentUserContext.UserId);

        // Update vehicle status to UnderMaintenance
        if (record.Vehicle.Status != VehicleStatus.UnderMaintenance)
        {
            try
            {
                await _vehicleService.ChangeStatusAsync(
                    record.VehicleId,
                    new ChangeVehicleStatusRequest(VehicleStatus.UnderMaintenance, $"Started maintenance service: {record.MaintenanceServiceType.Name}"),
                    cancellationToken);
            }
            catch
            {
                // Fallback direct update if status validation restricts
                record.Vehicle.SetStatus(VehicleStatus.UnderMaintenance);
            }
        }

        // Create downtime record
        var downtime = new VehicleDowntimeRecord(
            tenantId,
            record.VehicleId,
            startTime,
            $"Maintenance in progress: {record.MaintenanceServiceType.Name}",
            DowntimeType.Maintenance,
            record.Id);

        _dbContext.VehicleDowntimeRecords.Add(downtime);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceRecordStarted,
                "VehicleMaintenanceRecord",
                record.Id.ToString(),
                $"Started maintenance service for vehicle {record.Vehicle.VehicleNumber}"),
            cancellationToken);

        return await GetByIdAsync(record.Id, cancellationToken);
    }

    public async Task<VehicleMaintenanceRecordDto> CompleteServiceAsync(
        Guid id,
        CompleteMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var record = await _dbContext.VehicleMaintenanceRecords
                .Include(x => x.Vehicle)
                .Include(x => x.MaintenanceServiceType)
                .Include(x => x.Parts.Where(p => !p.IsDeleted))
                .Include(x => x.Labour.Where(l => !l.IsDeleted))
                .Include(x => x.Expenses.Where(e => !e.IsDeleted))
                .Include(x => x.DowntimeRecords.Where(dt => !dt.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

            if (record is null)
                throw new KeyNotFoundException($"Maintenance record '{id}' was not found.");

            if (record.Status == MaintenanceRecordStatus.Completed)
                throw new InvalidOperationException("Maintenance record is already completed.");

            // 1. Complete record & recalculate totals
            record.CompleteService(
                request.CompletedDateTimeUtc,
                request.FinalOdometer,
                request.FinalEngineHours,
                _currentUserContext.UserId,
                request.Notes);

            // 2. Odometer integration through Phase 3 IVehicleOdometerService
            if (request.FinalOdometer.HasValue && request.FinalOdometer.Value >= record.Vehicle.CurrentOdometer)
            {
                await _odometerService.RecordOdometerAsync(
                    record.VehicleId,
                    new RecordVehicleOdometerRequest(
                        request.FinalOdometer.Value,
                        record.OdometerUnit,
                        request.CompletedDateTimeUtc,
                        OdometerSource.Maintenance,
                        $"Maintenance service completed ({record.MaintenanceServiceType.Name})"),
                    cancellationToken);
            }

            // 3. Close open downtime records
            var openDowntime = record.DowntimeRecords.FirstOrDefault(x => x.EndedAtUtc == null);
            if (openDowntime is not null)
            {
                openDowntime.EndDowntime(request.CompletedDateTimeUtc, "Service completed", _currentUserContext.UserId);
            }

            // 4. Restore vehicle status to InService / Active
            if (record.Vehicle.Status == VehicleStatus.UnderMaintenance)
            {
                try
                {
                    await _vehicleService.ChangeStatusAsync(
                        record.VehicleId,
                        new ChangeVehicleStatusRequest(VehicleStatus.InService, $"Completed maintenance service: {record.MaintenanceServiceType.Name}"),
                        cancellationToken);
                }
                catch
                {
                    record.Vehicle.SetStatus(VehicleStatus.InService);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 5. Recalculate schedule due projection in background / post-commit
            await _dueEvaluationService.EvaluateAndRefreshProjectionsAsync(record.VehicleId, cancellationToken);

            await _auditLogService.WriteAsync(
                new CreateAuditLogRequest(
                    AuditAction.MaintenanceRecordCompleted,
                    "VehicleMaintenanceRecord",
                    record.Id.ToString(),
                    $"Completed maintenance service for vehicle {record.Vehicle.VehicleNumber}. Total cost: {record.TotalCost} {record.CurrencyCode}"),
                cancellationToken);

            return await GetByIdAsync(record.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VehicleMaintenanceRecordDto> CancelServiceAsync(
        Guid id,
        CancelMaintenanceRecordRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Vehicle)
            .Include(x => x.DowntimeRecords)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{id}' was not found.");

        record.CancelService(request?.Reason, _currentUserContext.UserId);

        // Close any open downtime
        var openDowntime = record.DowntimeRecords.FirstOrDefault(x => x.EndedAtUtc == null);
        if (openDowntime is not null)
        {
            openDowntime.EndDowntime(DateTime.UtcNow, $"Service cancelled: {request?.Reason}", _currentUserContext.UserId);
        }

        // Restore vehicle status if UnderMaintenance
        if (record.Vehicle.Status == VehicleStatus.UnderMaintenance)
        {
            try
            {
                await _vehicleService.ChangeStatusAsync(
                    record.VehicleId,
                    new ChangeVehicleStatusRequest(VehicleStatus.InService, $"Cancelled maintenance service: {request?.Reason}"),
                    cancellationToken);
            }
            catch
            {
                record.Vehicle.SetStatus(VehicleStatus.InService);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceRecordCancelled,
                "VehicleMaintenanceRecord",
                record.Id.ToString(),
                $"Cancelled maintenance record {record.Id}. Reason: {request?.Reason}"),
            cancellationToken);

        return await GetByIdAsync(record.Id, cancellationToken);
    }

    // Tasks
    public async Task<VehicleMaintenanceTaskDto> AddTaskAsync(
        Guid recordId,
        AddMaintenanceTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{recordId}' was not found.");

        var task = new VehicleMaintenanceTask(
            tenantId,
            recordId,
            request.Name,
            request.MaintenanceServiceTypeId,
            request.Description,
            MaintenanceTaskStatus.Pending,
            request.Notes);

        _dbContext.VehicleMaintenanceTasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceTaskAdded,
                "VehicleMaintenanceTask",
                task.Id.ToString(),
                $"Added task '{task.Name}' to record {recordId}"),
            cancellationToken);

        return new VehicleMaintenanceTaskDto(
            task.Id,
            task.MaintenanceRecordId,
            task.MaintenanceServiceTypeId,
            null,
            task.Name,
            task.Description,
            task.Status,
            task.Status.ToString(),
            task.CompletedAtUtc,
            task.CompletedByUserId,
            null,
            task.Notes);
    }

    public async Task<VehicleMaintenanceTaskDto> UpdateTaskStatusAsync(
        Guid recordId,
        Guid taskId,
        UpdateMaintenanceTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var task = await _dbContext.VehicleMaintenanceTasks
            .Include(x => x.MaintenanceServiceType)
            .FirstOrDefaultAsync(x => x.Id == taskId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Maintenance task '{taskId}' was not found.");

        task.UpdateStatus(request.Status, _currentUserContext.UserId, request.Notes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceTaskCompleted,
                "VehicleMaintenanceTask",
                task.Id.ToString(),
                $"Updated task '{task.Name}' status to {task.Status}"),
            cancellationToken);

        return new VehicleMaintenanceTaskDto(
            task.Id,
            task.MaintenanceRecordId,
            task.MaintenanceServiceTypeId,
            task.MaintenanceServiceType?.Name,
            task.Name,
            task.Description,
            task.Status,
            task.Status.ToString(),
            task.CompletedAtUtc,
            task.CompletedByUserId,
            null,
            task.Notes);
    }

    public async Task DeleteTaskAsync(
        Guid recordId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var task = await _dbContext.VehicleMaintenanceTasks
            .FirstOrDefaultAsync(x => x.Id == taskId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Maintenance task '{taskId}' was not found.");

        task.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceTaskRemoved,
                "VehicleMaintenanceTask",
                task.Id.ToString(),
                $"Deleted task '{task.Name}' from record {recordId}"),
            cancellationToken);
    }

    // Parts
    public async Task<VehicleMaintenancePartDto> AddPartAsync(
        Guid recordId,
        AddMaintenancePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{recordId}' was not found.");

        var part = new VehicleMaintenancePart(
            tenantId,
            recordId,
            request.PartName,
            request.Quantity,
            request.PartNumber,
            request.Unit,
            request.UnitCost,
            request.Supplier,
            request.Notes);

        _dbContext.VehicleMaintenanceParts.Add(part);
        await _dbContext.SaveChangesAsync(cancellationToken);

        record.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePartAdded,
                "VehicleMaintenancePart",
                part.Id.ToString(),
                $"Added part '{part.PartName}' (qty: {part.Quantity}, cost: {part.TotalCost}) to record {recordId}"),
            cancellationToken);

        return new VehicleMaintenancePartDto(
            part.Id,
            part.MaintenanceRecordId,
            part.PartNumber,
            part.PartName,
            part.Quantity,
            part.Unit,
            part.UnitCost,
            part.TotalCost,
            part.Supplier,
            part.Notes);
    }

    public async Task DeletePartAsync(
        Guid recordId,
        Guid partId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var part = await _dbContext.VehicleMaintenanceParts
            .FirstOrDefaultAsync(x => x.Id == partId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (part is null)
            throw new KeyNotFoundException($"Maintenance part '{partId}' was not found.");

        part.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        record?.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenancePartRemoved,
                "VehicleMaintenancePart",
                part.Id.ToString(),
                $"Removed part '{part.PartName}' from record {recordId}"),
            cancellationToken);
    }

    // Labour
    public async Task<VehicleMaintenanceLabourDto> AddLabourAsync(
        Guid recordId,
        AddMaintenanceLabourRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{recordId}' was not found.");

        if (request.EmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.EmployeeId.Value && x.TenantId == tenantId, cancellationToken);

            if (!empExists)
                throw new KeyNotFoundException($"Employee '{request.EmployeeId.Value}' was not found for this tenant.");
        }

        var labour = new VehicleMaintenanceLabour(
            tenantId,
            recordId,
            request.Description,
            request.Hours,
            request.HourlyRate,
            request.TechnicianName,
            request.EmployeeId,
            request.Notes);

        _dbContext.VehicleMaintenanceLabours.Add(labour);
        await _dbContext.SaveChangesAsync(cancellationToken);

        record.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceLabourAdded,
                "VehicleMaintenanceLabour",
                labour.Id.ToString(),
                $"Added labour '{labour.Description}' ({labour.Hours} hrs, cost: {labour.TotalCost}) to record {recordId}"),
            cancellationToken);

        return new VehicleMaintenanceLabourDto(
            labour.Id,
            labour.MaintenanceRecordId,
            labour.Description,
            labour.Hours,
            labour.HourlyRate,
            labour.TotalCost,
            labour.TechnicianName,
            labour.EmployeeId,
            null,
            labour.Notes);
    }

    public async Task DeleteLabourAsync(
        Guid recordId,
        Guid labourId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var labour = await _dbContext.VehicleMaintenanceLabours
            .FirstOrDefaultAsync(x => x.Id == labourId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (labour is null)
            throw new KeyNotFoundException($"Maintenance labour '{labourId}' was not found.");

        labour.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        record?.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceLabourRemoved,
                "VehicleMaintenanceLabour",
                labour.Id.ToString(),
                $"Removed labour '{labour.Description}' from record {recordId}"),
            cancellationToken);
    }

    // Expenses
    public async Task<VehicleMaintenanceExpenseDto> AddExpenseAsync(
        Guid recordId,
        AddMaintenanceExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{recordId}' was not found.");

        var expense = new VehicleMaintenanceExpense(
            tenantId,
            recordId,
            request.ExpenseType,
            request.Description,
            request.Amount,
            request.CurrencyCode,
            request.Reference,
            request.Notes);

        _dbContext.VehicleMaintenanceExpenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        record.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceExpenseAdded,
                "VehicleMaintenanceExpense",
                expense.Id.ToString(),
                $"Added expense '{expense.Description}' ({expense.Amount} {expense.CurrencyCode}) to record {recordId}"),
            cancellationToken);

        return new VehicleMaintenanceExpenseDto(
            expense.Id,
            expense.MaintenanceRecordId,
            expense.ExpenseType,
            expense.ExpenseType.ToString(),
            expense.Description,
            expense.Amount,
            expense.CurrencyCode,
            expense.Reference,
            expense.Notes);
    }

    public async Task DeleteExpenseAsync(
        Guid recordId,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var expense = await _dbContext.VehicleMaintenanceExpenses
            .FirstOrDefaultAsync(x => x.Id == expenseId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (expense is null)
            throw new KeyNotFoundException($"Maintenance expense '{expenseId}' was not found.");

        expense.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var record = await _dbContext.VehicleMaintenanceRecords
            .Include(x => x.Parts.Where(p => !p.IsDeleted))
            .Include(x => x.Labour.Where(l => !l.IsDeleted))
            .Include(x => x.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        record?.RecalculateTotals();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceExpenseRemoved,
                "VehicleMaintenanceExpense",
                expense.Id.ToString(),
                $"Removed expense '{expense.Description}' from record {recordId}"),
            cancellationToken);
    }

    // Documents
    public async Task<VehicleMaintenanceDocumentDto> AddDocumentAsync(
        Guid recordId,
        AddMaintenanceDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var record = await _dbContext.VehicleMaintenanceRecords
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException($"Maintenance record '{recordId}' was not found.");

        var doc = new VehicleMaintenanceDocument(
            tenantId,
            recordId,
            request.DocumentType,
            request.Title,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            DateTime.UtcNow,
            _currentUserContext.UserId,
            request.Notes);

        _dbContext.VehicleMaintenanceDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceDocumentAdded,
                "VehicleMaintenanceDocument",
                doc.Id.ToString(),
                $"Added document '{doc.Title}' ({doc.DocumentType}) to record {recordId}"),
            cancellationToken);

        return new VehicleMaintenanceDocumentDto(
            doc.Id,
            doc.MaintenanceRecordId,
            doc.DocumentType,
            doc.DocumentType.ToString(),
            doc.Title,
            doc.FileObjectKey,
            doc.FileName,
            doc.ContentType,
            doc.FileSizeBytes,
            doc.UploadedAtUtc,
            doc.UploadedByUserId,
            null,
            doc.Notes);
    }

    public async Task DeleteDocumentAsync(
        Guid recordId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.VehicleMaintenanceDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.MaintenanceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Maintenance document '{documentId}' was not found.");

        doc.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceDocumentRemoved,
                "VehicleMaintenanceDocument",
                doc.Id.ToString(),
                $"Removed document '{doc.Title}' from record {recordId}"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static VehicleMaintenanceRecordListItemDto MapToListItemDto(VehicleMaintenanceRecord entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.VehicleId,
            entity.Vehicle.VehicleNumber,
            entity.Vehicle.RegistrationNumber,
            entity.Vehicle.DisplayName,
            entity.MaintenanceServiceTypeId,
            entity.MaintenanceServiceType.Code,
            entity.MaintenanceServiceType.Name,
            entity.MaintenanceServiceType.Category,
            entity.MaintenanceServiceType.Category.ToString(),
            entity.MaintenancePlanId,
            entity.MaintenancePlan?.Name,
            entity.MaintenanceProviderId,
            entity.MaintenanceProvider?.Name,
            entity.ServiceDateUtc,
            entity.StartDateTimeUtc,
            entity.CompletedDateTimeUtc,
            entity.OdometerReading,
            entity.OdometerUnit,
            entity.EngineHours,
            entity.MaintenanceType,
            entity.MaintenanceType.ToString(),
            entity.Status,
            entity.Status.ToString(),
            entity.ReferenceNumber,
            entity.TotalCost ?? 0m,
            entity.CurrencyCode ?? "USD",
            entity.VehicleDowntimeMinutes,
            entity.Tasks.Count(t => !t.IsDeleted),
            entity.Parts.Count(p => !p.IsDeleted),
            entity.Documents.Count(d => !d.IsDeleted),
            entity.CreatedAtUtc);

    private static VehicleMaintenanceRecordDto MapToDetailDto(VehicleMaintenanceRecord entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.VehicleId,
            entity.Vehicle.VehicleNumber,
            entity.Vehicle.RegistrationNumber,
            entity.Vehicle.DisplayName,
            entity.MaintenanceServiceTypeId,
            entity.MaintenanceServiceType.Code,
            entity.MaintenanceServiceType.Name,
            entity.MaintenanceServiceType.Category,
            entity.MaintenanceServiceType.Category.ToString(),
            entity.MaintenancePlanId,
            entity.MaintenancePlan?.Name,
            entity.MaintenancePlanRuleId,
            entity.MaintenanceProviderId,
            entity.MaintenanceProvider?.Name,
            entity.ServiceDateUtc,
            entity.StartDateTimeUtc,
            entity.CompletedDateTimeUtc,
            entity.OdometerReading,
            entity.OdometerUnit,
            entity.EngineHours,
            entity.MaintenanceType,
            entity.MaintenanceType.ToString(),
            entity.Status,
            entity.Status.ToString(),
            entity.ReferenceNumber,
            entity.Description,
            entity.TechnicianNotes,
            entity.TotalPartsCost,
            entity.TotalLabourCost,
            entity.OtherCost,
            entity.TotalCost ?? 0m,
            entity.CurrencyCode ?? "USD",
            entity.VehicleDowntimeMinutes,
            entity.CreatedByUserId,
            null,
            entity.CompletedByUserId,
            null,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Tasks.Where(t => !t.IsDeleted).Select(t => new VehicleMaintenanceTaskDto(
                t.Id,
                t.MaintenanceRecordId,
                t.MaintenanceServiceTypeId,
                t.MaintenanceServiceType?.Name,
                t.Name,
                t.Description,
                t.Status,
                t.Status.ToString(),
                t.CompletedAtUtc,
                t.CompletedByUserId,
                null,
                t.Notes)).ToList(),
            entity.Parts.Where(p => !p.IsDeleted).Select(p => new VehicleMaintenancePartDto(
                p.Id,
                p.MaintenanceRecordId,
                p.PartNumber,
                p.PartName,
                p.Quantity,
                p.Unit,
                p.UnitCost,
                p.TotalCost,
                p.Supplier,
                p.Notes)).ToList(),
            entity.Labour.Where(l => !l.IsDeleted).Select(l => new VehicleMaintenanceLabourDto(
                l.Id,
                l.MaintenanceRecordId,
                l.Description,
                l.Hours,
                l.HourlyRate,
                l.TotalCost,
                l.TechnicianName,
                l.EmployeeId,
                l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : null,
                l.Notes)).ToList(),
            entity.Expenses.Where(e => !e.IsDeleted).Select(e => new VehicleMaintenanceExpenseDto(
                e.Id,
                e.MaintenanceRecordId,
                e.ExpenseType,
                e.ExpenseType.ToString(),
                e.Description,
                e.Amount,
                e.CurrencyCode,
                e.Reference,
                e.Notes)).ToList(),
            entity.Documents.Where(d => !d.IsDeleted).Select(d => new VehicleMaintenanceDocumentDto(
                d.Id,
                d.MaintenanceRecordId,
                d.DocumentType,
                d.DocumentType.ToString(),
                d.Title,
                d.FileObjectKey,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedAtUtc,
                d.UploadedByUserId,
                null,
                d.Notes)).ToList(),
            entity.DowntimeRecords.Where(dt => !dt.IsDeleted).Select(dt => new VehicleDowntimeRecordDto(
                dt.Id,
                dt.VehicleId,
                dt.MaintenanceRecordId,
                dt.StartedAtUtc,
                dt.EndedAtUtc,
                dt.DurationMinutes,
                dt.DowntimeType,
                dt.DowntimeType.ToString(),
                dt.Reason,
                dt.Notes)).ToList());
}

using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetCustodyService : IAssetCustodyService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetCustodyService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<AssetEmployeeAssignmentDto> AssignToEmployeeAsync(AssignAssetToEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.TenantId == tenantId, cancellationToken);
        if (employee is null)
            throw new ValidationException($"Employee '{request.EmployeeId}' was not found.");

        var activeAssignments = await _dbContext.AssetEmployeeAssignments
            .Where(x => x.AssetId == request.AssetId && x.TenantId == tenantId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in activeAssignments)
        {
            active.EndAssignment(
                DateTime.UtcNow,
                request.ConditionAtAssignment,
                "Automatic return due to reassignment",
                _currentUserContext.UserId);
        }

        var assignment = new AssetEmployeeAssignment(
            tenantId,
            asset.Id,
            employee.Id,
            DateTime.UtcNow,
            request.ExpectedReturnDateUtc,
            AssetAssignmentType.Permanent,
            request.ConditionAtAssignment,
            request.Notes,
            true,
            _currentUserContext.UserId);

        _dbContext.AssetEmployeeAssignments.Add(assignment);

        asset.SetCustodian(employee.Id, _currentUserContext.UserId);
        asset.SetCondition(request.ConditionAtAssignment, _currentUserContext.UserId);

        var conditionRecord = new AssetConditionRecord(
            tenantId,
            asset.Id,
            request.ConditionAtAssignment,
            DateTime.UtcNow,
            employee.Id,
            null,
            "Employee Assignment Handover",
            null,
            _currentUserContext.UserId);
        _dbContext.AssetConditionRecords.Add(conditionRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetEmployeeAssigned,
            nameof(AssetEmployeeAssignment),
            assignment.Id.ToString(),
            $"Assigned asset {asset.AssetNumber} to employee {employee.FirstName} {employee.LastName}",
            null,
            null), cancellationToken);

        return (await GetEmployeeAssignmentByIdAsync(assignment.Id, cancellationToken))!;
    }

    public async Task<AssetEmployeeAssignmentDto> ReturnFromEmployeeAsync(Guid assignmentId, ReturnAssetFromEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var assignment = await _dbContext.AssetEmployeeAssignments
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.TenantId == tenantId, cancellationToken);
        if (assignment is null)
            throw new KeyNotFoundException($"Assignment '{assignmentId}' was not found.");

        if (!assignment.IsActive)
            throw new ValidationException("Assignment is already ended.");

        assignment.EndAssignment(
            DateTime.UtcNow,
            request.ConditionAtReturn,
            request.Notes,
            _currentUserContext.UserId);

        var asset = assignment.Asset;
        asset.SetCustodian(null, _currentUserContext.UserId);
        asset.SetCondition(request.ConditionAtReturn, _currentUserContext.UserId);

        var conditionRecord = new AssetConditionRecord(
            tenantId,
            asset.Id,
            request.ConditionAtReturn,
            DateTime.UtcNow,
            assignment.EmployeeId,
            null,
            "Employee Return Handover",
            null,
            _currentUserContext.UserId);
        _dbContext.AssetConditionRecords.Add(conditionRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetEmployeeReturned,
            nameof(AssetEmployeeAssignment),
            assignment.Id.ToString(),
            $"Returned asset {asset.AssetNumber} from employee {assignment.Employee?.FirstName} {assignment.Employee?.LastName}",
            null,
            null), cancellationToken);

        return (await GetEmployeeAssignmentByIdAsync(assignment.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<AssetEmployeeAssignmentDto>> GetEmployeeAssignmentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetEmployeeAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToEmployeeAssignmentDto).ToList();
    }

    public async Task<IReadOnlyList<AssetEmployeeAssignmentDto>> GetActiveAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetEmployeeAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Where(x => x.TenantId == tenantId && x.EmployeeId == employeeId && x.IsActive)
            .OrderByDescending(x => x.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToEmployeeAssignmentDto).ToList();
    }

    public async Task<AssetVehicleAssignmentDto> AssignToVehicleAsync(AssignAssetToVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId, cancellationToken);
        if (vehicle is null)
            throw new ValidationException($"Vehicle '{request.VehicleId}' was not found.");

        var activeVehicleAssignments = await _dbContext.AssetVehicleAssignments
            .Where(x => x.AssetId == request.AssetId && x.TenantId == tenantId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in activeVehicleAssignments)
        {
            active.EndAssignment(
                DateTime.UtcNow,
                "Automatic removal due to vehicle reassignment",
                _currentUserContext.UserId);
        }

        var assignment = new AssetVehicleAssignment(
            tenantId,
            asset.Id,
            vehicle.Id,
            DateTime.UtcNow,
            null,
            request.Notes,
            true,
            _currentUserContext.UserId);

        _dbContext.AssetVehicleAssignments.Add(assignment);

        asset.SetAssignedVehicle(vehicle.Id, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetVehicleAssigned,
            nameof(AssetVehicleAssignment),
            assignment.Id.ToString(),
            $"Assigned asset {asset.AssetNumber} to vehicle {vehicle.RegistrationNumber ?? vehicle.VehicleNumber}",
            null,
            null), cancellationToken);

        return (await GetVehicleAssignmentByIdAsync(assignment.Id, cancellationToken))!;
    }

    public async Task<AssetVehicleAssignmentDto> RemoveFromVehicleAsync(Guid assignmentId, RemoveAssetFromVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var assignment = await _dbContext.AssetVehicleAssignments
            .Include(x => x.Asset)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.TenantId == tenantId, cancellationToken);
        if (assignment is null)
            throw new KeyNotFoundException($"Vehicle assignment '{assignmentId}' was not found.");

        if (!assignment.IsActive)
            throw new ValidationException("Assignment is already ended.");

        assignment.EndAssignment(
            DateTime.UtcNow,
            request.Notes,
            _currentUserContext.UserId);

        var asset = assignment.Asset;
        asset.SetAssignedVehicle(null, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetVehicleReturned,
            nameof(AssetVehicleAssignment),
            assignment.Id.ToString(),
            $"Removed asset {asset.AssetNumber} from vehicle {assignment.Vehicle?.RegistrationNumber ?? assignment.Vehicle?.VehicleNumber}",
            null,
            null), cancellationToken);

        return (await GetVehicleAssignmentByIdAsync(assignment.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<AssetVehicleAssignmentDto>> GetVehicleAssignmentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetVehicleAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToVehicleAssignmentDto).ToList();
    }

    public async Task<IReadOnlyList<AssetVehicleAssignmentDto>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetVehicleAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId && x.IsActive)
            .OrderByDescending(x => x.AssignedFromUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToVehicleAssignmentDto).ToList();
    }

    public async Task<AssetUsageSessionDto> CheckOutAsync(StartAssetUsageSessionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.TenantId == tenantId, cancellationToken);
        if (employee is null)
            throw new ValidationException($"Employee '{request.EmployeeId}' was not found.");

        if (request.VehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles
                .AnyAsync(x => x.Id == request.VehicleId.Value && x.TenantId == tenantId, cancellationToken);
            if (!vehicleExists)
                throw new ValidationException($"Vehicle '{request.VehicleId.Value}' was not found.");
        }

        var hasOpenSession = await _dbContext.AssetUsageSessions
            .AnyAsync(x => x.AssetId == request.AssetId && x.TenantId == tenantId && x.Status == AssetUsageSessionStatus.Open, cancellationToken);
        if (hasOpenSession)
            throw new ConflictException($"Asset {asset.AssetNumber} is already checked out in an open session.");

        var session = new AssetUsageSession(
            tenantId,
            asset.Id,
            employee.Id,
            request.VehicleId,
            DateTime.UtcNow,
            request.ExpectedReturnUtc,
            request.Condition,
            request.Purpose,
            null,
            request.Notes,
            _currentUserContext.UserId);

        _dbContext.AssetUsageSessions.Add(session);

        asset.ChangeStatus(AssetStatus.InUse, _currentUserContext.UserId);
        asset.SetCondition(request.Condition, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCheckedOut,
            nameof(AssetUsageSession),
            session.Id.ToString(),
            $"Checked out asset {asset.AssetNumber} to {employee.FirstName} {employee.LastName}",
            null,
            null), cancellationToken);

        return (await GetUsageSessionByIdAsync(session.Id, cancellationToken))!;
    }

    public async Task<AssetUsageSessionDto> CheckInAsync(Guid sessionId, EndAssetUsageSessionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var session = await _dbContext.AssetUsageSessions
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException($"Usage session '{sessionId}' was not found.");

        if (session.Status != AssetUsageSessionStatus.Open)
            throw new ValidationException("Session is already closed or cancelled.");

        session.CompleteCheckin(
            DateTime.UtcNow,
            request.Condition,
            request.Notes,
            _currentUserContext.UserId);

        var asset = session.Asset;
        asset.ChangeStatus(AssetStatus.Available, _currentUserContext.UserId);
        asset.SetCondition(request.Condition, _currentUserContext.UserId);

        var conditionRecord = new AssetConditionRecord(
            tenantId,
            asset.Id,
            request.Condition,
            DateTime.UtcNow,
            session.EmployeeId,
            session.Id,
            "Usage Session Check-In",
            null,
            _currentUserContext.UserId);
        _dbContext.AssetConditionRecords.Add(conditionRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCheckedIn,
            nameof(AssetUsageSession),
            session.Id.ToString(),
            $"Checked in asset {asset.AssetNumber} from {session.Employee?.FirstName} {session.Employee?.LastName}",
            null,
            null), cancellationToken);

        return (await GetUsageSessionByIdAsync(session.Id, cancellationToken))!;
    }

    public async Task<PagedResult<AssetUsageSessionDto>> GetUsageSessionsPagedAsync(
        Guid? assetId = null,
        Guid? employeeId = null,
        Guid? vehicleId = null,
        bool? activeOnly = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.AssetUsageSessions
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId);

        if (assetId.HasValue) query = query.Where(x => x.AssetId == assetId.Value);
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        if (activeOnly.HasValue && activeOnly.Value) query = query.Where(x => x.Status == AssetUsageSessionStatus.Open);

        var totalCount = await query.CountAsync(cancellationToken);

        var validPageNumber = Math.Max(1, pageNumber);
        var validPageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CheckedOutAtUtc)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToUsageSessionDto).ToList();

        return new PagedResult<AssetUsageSessionDto>(dtos, totalCount, validPageNumber, validPageSize);
    }

    public async Task<AssetUsageSessionDto?> GetActiveSessionByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var session = await _dbContext.AssetUsageSessions
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.AssetId == assetId && x.TenantId == tenantId && x.Status == AssetUsageSessionStatus.Open, cancellationToken);

        if (session is null) return null;
        return MapToUsageSessionDto(session);
    }

    private async Task<AssetEmployeeAssignmentDto?> GetEmployeeAssignmentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var item = await _dbContext.AssetEmployeeAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;
        return MapToEmployeeAssignmentDto(item);
    }

    private async Task<AssetVehicleAssignmentDto?> GetVehicleAssignmentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var item = await _dbContext.AssetVehicleAssignments
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;
        return MapToVehicleAssignmentDto(item);
    }

    private async Task<AssetUsageSessionDto?> GetUsageSessionByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var item = await _dbContext.AssetUsageSessions
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;
        return MapToUsageSessionDto(item);
    }

    private static AssetEmployeeAssignmentDto MapToEmployeeAssignmentDto(AssetEmployeeAssignment x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.EmployeeId,
        x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}" : null,
        x.Employee?.EmployeeNumber,
        x.AssignedFromUtc,
        x.AssignedByUserId?.ToString(),
        null,
        x.ConditionAtAssignment ?? AssetCondition.Good,
        x.AssignedToUtc,
        x.IsActive ? null : x.AssignedToUtc,
        x.ReturnedByUserId?.ToString(),
        null,
        x.ConditionAtReturn,
        x.IsActive,
        x.Notes);

    private static AssetVehicleAssignmentDto MapToVehicleAssignmentDto(AssetVehicleAssignment x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.VehicleId,
        x.Vehicle != null ? (x.Vehicle.RegistrationNumber ?? x.Vehicle.VehicleNumber) : null,
        null,
        x.AssignedFromUtc,
        x.AssignedByUserId?.ToString(),
        null,
        x.IsActive ? null : x.AssignedToUtc,
        x.EndedByUserId?.ToString(),
        null,
        x.IsActive,
        x.Notes);

    private static AssetUsageSessionDto MapToUsageSessionDto(AssetUsageSession x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.EmployeeId ?? Guid.Empty,
        x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}" : null,
        x.VehicleId,
        x.Vehicle != null ? (x.Vehicle.RegistrationNumber ?? x.Vehicle.VehicleNumber) : null,
        x.CheckedOutAtUtc,
        x.CheckedOutByUserId?.ToString(),
        null,
        null,
        x.ConditionAtCheckout,
        x.ExpectedReturnAtUtc,
        x.CheckedInAtUtc,
        x.CheckedInByUserId?.ToString(),
        null,
        null,
        x.ConditionAtCheckin,
        x.Status,
        x.Purpose,
        x.Notes,
        x.CheckedInAtUtc.HasValue ? (decimal)(x.CheckedInAtUtc.Value - x.CheckedOutAtUtc).TotalHours : null);
}

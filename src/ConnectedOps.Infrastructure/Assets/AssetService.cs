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

public sealed class AssetService : IAssetService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetService(
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

    public async Task<PagedResult<AssetDto>> GetPagedAsync(AssetListFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Include(x => x.AssetType)
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.CurrentCustodianEmployee)
            .Include(x => x.CurrentAssignedVehicle)
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.AssetNumber.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(search)) ||
                (x.InternalCode != null && x.InternalCode.ToLower().Contains(search)) ||
                (x.Manufacturer != null && x.Manufacturer.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.ToLower().Contains(search)));
        }

        if (filter.CategoryId.HasValue) query = query.Where(x => x.AssetCategoryId == filter.CategoryId.Value);
        if (filter.AssetTypeId.HasValue) query = query.Where(x => x.AssetTypeId == filter.AssetTypeId.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.Condition.HasValue) query = query.Where(x => x.Condition == filter.Condition.Value);
        if (filter.BranchId.HasValue) query = query.Where(x => x.BranchId == filter.BranchId.Value);
        if (filter.LocationId.HasValue) query = query.Where(x => x.LocationId == filter.LocationId.Value);
        if (filter.DepartmentId.HasValue) query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        if (filter.TeamId.HasValue) query = query.Where(x => x.TeamId == filter.TeamId.Value);
        if (filter.CustodianEmployeeId.HasValue) query = query.Where(x => x.CurrentCustodianEmployeeId == filter.CustodianEmployeeId.Value);
        if (filter.AssignedVehicleId.HasValue) query = query.Where(x => x.CurrentAssignedVehicleId == filter.AssignedVehicleId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<AssetDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<AssetDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var item = await _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Include(x => x.AssetType)
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.CurrentCustodianEmployee)
            .Include(x => x.CurrentAssignedVehicle)
            .Include(x => x.Identifiers)
            .Include(x => x.LocationHistories).ThenInclude(lh => lh.Branch)
            .Include(x => x.LocationHistories).ThenInclude(lh => lh.Location)
            .Include(x => x.NotesList)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;

        return MapToDetailDto(item);
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var assetNumber = request.AssetNumber.Trim().ToUpperInvariant();

        var exists = await _dbContext.Assets
            .AnyAsync(x => x.TenantId == tenantId && x.AssetNumber == assetNumber, cancellationToken);
        if (exists)
            throw new ConflictException($"Asset with number '{assetNumber}' already exists.");

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var snExists = await _dbContext.Assets
                .AnyAsync(x => x.TenantId == tenantId && x.SerialNumber == request.SerialNumber.Trim(), cancellationToken);
            if (snExists)
                throw new ConflictException($"Asset with serial number '{request.SerialNumber.Trim()}' already exists.");
        }

        var catExists = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.CategoryId, cancellationToken);
        if (!catExists)
            throw new ValidationException("Selected category does not exist.");

        var asset = new Asset(
            tenantId,
            assetNumber,
            request.Name.Trim(),
            request.CategoryId,
            request.AssetTypeId,
            request.InternalCode?.Trim(),
            request.Description?.Trim(),
            request.SerialNumber?.Trim(),
            request.Make?.Trim(),
            request.Model?.Trim(),
            request.OwnershipType,
            AssetStatus.Available,
            request.Condition,
            request.BranchId,
            request.LocationId,
            request.DepartmentId,
            request.TeamId,
            request.PurchaseDate.HasValue ? DateOnly.FromDateTime(request.PurchaseDate.Value) : null,
            request.PurchaseCost,
            "USD",
            request.VendorName?.Trim(),
            null,
            null,
            request.WarrantyExpiryDate.HasValue ? DateOnly.FromDateTime(request.WarrantyExpiryDate.Value) : null,
            request.VendorName?.Trim(),
            request.WarrantyNotes?.Trim(),
            DateTime.UtcNow,
            null,
            null,
            false,
            true,
            _currentUserContext.UserId);

        _dbContext.Assets.Add(asset);

        // Auto create primary QR identifier
        var publicToken = Guid.NewGuid().ToString("N");
        var qrIdentifier = new AssetIdentifier(
            tenantId,
            asset.Id,
            AssetIdentifierType.QRCode,
            assetNumber,
            publicToken,
            isActive: true);
        _dbContext.AssetIdentifiers.Add(qrIdentifier);

        // Initial condition record
        var initialCondition = new AssetConditionRecord(
            tenantId,
            asset.Id,
            request.Condition,
            DateTime.UtcNow,
            null,
            null,
            "Initial asset condition at registration",
            null,
            _currentUserContext.UserId);
        _dbContext.AssetConditionRecords.Add(initialCondition);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCreated,
            nameof(Asset),
            asset.Id.ToString(),
            $"Created asset '{asset.Name}' ({asset.AssetNumber})",
            null,
            null), cancellationToken);

        var result = await GetByIdAsync(asset.Id, cancellationToken);
        return MapToDtoFromDetail(result!);
    }

    public async Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{id}' was not found.");

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var snExists = await _dbContext.Assets
                .AnyAsync(x => x.TenantId == tenantId && x.SerialNumber == request.SerialNumber.Trim() && x.Id != id, cancellationToken);
            if (snExists)
                throw new ConflictException($"Asset with serial number '{request.SerialNumber.Trim()}' already exists.");
        }

        var catExists = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.CategoryId, cancellationToken);
        if (!catExists)
            throw new ValidationException("Selected category does not exist.");

        asset.UpdateDetails(
            asset.AssetNumber,
            request.Name.Trim(),
            request.CategoryId,
            request.AssetTypeId,
            request.InternalCode?.Trim(),
            request.Description?.Trim(),
            request.SerialNumber?.Trim(),
            request.Make?.Trim(),
            request.Model?.Trim(),
            request.OwnershipType,
            asset.BranchId,
            asset.LocationId,
            request.DepartmentId,
            request.TeamId,
            request.PurchaseDate.HasValue ? DateOnly.FromDateTime(request.PurchaseDate.Value) : null,
            request.PurchaseCost,
            asset.CurrencyCode,
            request.VendorName?.Trim(),
            asset.PurchaseReference,
            asset.WarrantyStartDate,
            request.WarrantyExpiryDate.HasValue ? DateOnly.FromDateTime(request.WarrantyExpiryDate.Value) : null,
            asset.WarrantyProvider,
            request.WarrantyNotes?.Trim(),
            asset.CommissionedAtUtc,
            asset.PrimaryImageObjectKey,
            asset.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetUpdated,
            nameof(Asset),
            asset.Id.ToString(),
            $"Updated asset '{asset.Name}' ({asset.AssetNumber})",
            null,
            null), cancellationToken);

        var result = await GetByIdAsync(asset.Id, cancellationToken);
        return MapToDtoFromDetail(result!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .Include(x => x.EmployeeAssignments)
            .Include(x => x.VehicleAssignments)
            .Include(x => x.UsageSessions)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{id}' was not found.");

        if (asset.EmployeeAssignments.Any(x => x.IsActive) ||
            asset.VehicleAssignments.Any(x => x.IsActive) ||
            asset.UsageSessions.Any(x => x.Status == AssetUsageSessionStatus.Open))
        {
            throw new ValidationException("Cannot delete asset that has active employee or vehicle assignments or an active checkout session.");
        }

        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetDeactivated,
            nameof(Asset),
            id.ToString(),
            $"Deleted asset '{asset.Name}' ({asset.AssetNumber})",
            null,
            null), cancellationToken);
    }

    public async Task<AssetDto> ChangeStatusAsync(Guid id, ChangeAssetStatusRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{id}' was not found.");

        var oldStatus = asset.Status;
        asset.ChangeStatus(request.NewStatus, _currentUserContext.UserId);

        var note = new AssetNote(
            tenantId,
            asset.Id,
            $"Status changed from {oldStatus} to {request.NewStatus}. Reason: {request.Reason}",
            _currentUserContext.UserId,
            _currentUserContext.Email);
        _dbContext.AssetNotes.Add(note);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetStatusChanged,
            nameof(Asset),
            asset.Id.ToString(),
            $"Status changed from {oldStatus} to {request.NewStatus} for asset {asset.AssetNumber}",
            oldStatus.ToString(),
            request.NewStatus.ToString()), cancellationToken);

        var result = await GetByIdAsync(asset.Id, cancellationToken);
        return MapToDtoFromDetail(result!);
    }

    public async Task<AssetDto> UpdateLocationAsync(Guid id, UpdateAssetLocationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{id}' was not found.");

        asset.AssignLocation(request.BranchId, request.LocationId, request.Reason, _currentUserContext.UserId);

        var history = new AssetLocationHistory(
            tenantId,
            asset.Id,
            request.BranchId,
            request.LocationId,
            DateTime.UtcNow,
            null,
            request.Reason ?? "Location update",
            _currentUserContext.UserId);
        _dbContext.AssetLocationHistories.Add(history);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetUpdated,
            nameof(Asset),
            asset.Id.ToString(),
            $"Updated location for asset '{asset.Name}' ({asset.AssetNumber})",
            null,
            null), cancellationToken);

        var result = await GetByIdAsync(asset.Id, cancellationToken);
        return MapToDtoFromDetail(result!);
    }

    public async Task<AssetNoteDto> AddNoteAsync(Guid id, AddAssetNoteRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var assetExists = await _dbContext.Assets
            .AnyAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (!assetExists)
            throw new KeyNotFoundException($"Asset '{id}' was not found.");

        var note = new AssetNote(
            tenantId,
            id,
            request.Note.Trim(),
            _currentUserContext.UserId,
            _currentUserContext.Email);

        _dbContext.AssetNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetUpdated,
            nameof(AssetNote),
            note.Id.ToString(),
            $"Added note to asset '{id}'",
            null,
            null), cancellationToken);

        return new AssetNoteDto(
            note.Id,
            note.AssetId,
            note.NoteText,
            note.CreatedAtUtc,
            note.CreatedByUserId?.ToString(),
            note.CreatedByUserName);
    }

    public async Task<IReadOnlyList<AssetNoteDto>> GetNotesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var notes = await _dbContext.AssetNotes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AssetId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return notes.Select(x => new AssetNoteDto(
            x.Id,
            x.AssetId,
            x.NoteText,
            x.CreatedAtUtc,
            x.CreatedByUserId?.ToString(),
            x.CreatedByUserName)).ToList();
    }

    public async Task<IReadOnlyList<AssetLocationHistoryDto>> GetLocationHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var items = await _dbContext.AssetLocationHistories
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Where(x => x.TenantId == tenantId && x.AssetId == id)
            .OrderByDescending(x => x.EffectiveFromUtc)
            .ToListAsync(cancellationToken);

        return items.Select(x => new AssetLocationHistoryDto(
            x.Id,
            x.AssetId,
            x.BranchId,
            x.Branch?.Name,
            x.LocationId,
            x.Location?.Name,
            x.EffectiveFromUtc,
            x.EffectiveToUtc,
            x.ChangedByUserId?.ToString(),
            null,
            x.Reason)).ToList();
    }

    private static AssetDto MapToDto(Asset x) => new(
        x.Id,
        x.AssetNumber,
        x.Name,
        x.SerialNumber,
        x.InternalCode,
        null,
        null,
        x.AssetCategoryId,
        x.AssetCategory?.Name,
        x.AssetTypeId ?? Guid.Empty,
        x.AssetType?.Name,
        x.Manufacturer,
        x.Model,
        null,
        x.OwnershipType,
        x.Status,
        x.Condition,
        AssetAssignmentType.Permanent,
        x.BranchId,
        x.Branch?.Name,
        x.LocationId,
        x.Location?.Name,
        x.DepartmentId,
        x.Department?.Name,
        x.TeamId,
        x.Team?.Name,
        x.CurrentCustodianEmployeeId,
        x.CurrentCustodianEmployee != null ? $"{x.CurrentCustodianEmployee.FirstName} {x.CurrentCustodianEmployee.LastName}" : null,
        x.CurrentAssignedVehicleId,
        x.CurrentAssignedVehicle != null ? (x.CurrentAssignedVehicle.RegistrationNumber ?? x.CurrentAssignedVehicle.VehicleNumber) : null,
        x.PurchaseCost,
        x.PurchaseDate.HasValue ? x.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue) : null,
        x.WarrantyEndDate.HasValue ? x.WarrantyEndDate.Value.ToDateTime(TimeOnly.MinValue) : null,
        null,
        null,
        0m,
        x.IsActive,
        x.CreatedAtUtc);

    private static AssetDetailDto MapToDetailDto(Asset x) => new(
        x.Id,
        x.AssetNumber,
        x.Name,
        x.Description,
        x.SerialNumber,
        x.InternalCode,
        null,
        null,
        x.AssetCategoryId,
        x.AssetCategory?.Name,
        x.AssetTypeId ?? Guid.Empty,
        x.AssetType?.Name,
        x.Manufacturer,
        x.Model,
        null,
        x.OwnershipType,
        x.Status,
        x.Condition,
        AssetAssignmentType.Permanent,
        x.BranchId,
        x.Branch?.Name,
        x.LocationId,
        x.Location?.Name,
        x.DepartmentId,
        x.Department?.Name,
        x.TeamId,
        x.Team?.Name,
        x.CurrentCustodianEmployeeId,
        x.CurrentCustodianEmployee != null ? $"{x.CurrentCustodianEmployee.FirstName} {x.CurrentCustodianEmployee.LastName}" : null,
        x.CurrentAssignedVehicleId,
        x.CurrentAssignedVehicle != null ? (x.CurrentAssignedVehicle.RegistrationNumber ?? x.CurrentAssignedVehicle.VehicleNumber) : null,
        x.PurchaseCost,
        x.PurchaseDate.HasValue ? x.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue) : null,
        x.SupplierName,
        x.WarrantyEndDate.HasValue ? x.WarrantyEndDate.Value.ToDateTime(TimeOnly.MinValue) : null,
        x.WarrantyReference,
        null,
        null,
        0m,
        null,
        false,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc,
        x.Identifiers?.Select(i => new AssetIdentifierDto(i.Id, i.AssetId, i.IdentifierType, i.Value, i.PublicToken, true, i.IsActive, i.CreatedAtUtc)).ToList(),
        x.LocationHistories?.Select(lh => new AssetLocationHistoryDto(lh.Id, lh.AssetId, lh.BranchId, lh.Branch?.Name, lh.LocationId, lh.Location?.Name, lh.EffectiveFromUtc, lh.EffectiveToUtc, lh.ChangedByUserId?.ToString(), null, lh.Reason)).ToList(),
        x.NotesList?.Select(n => new AssetNoteDto(n.Id, n.AssetId, n.NoteText, n.CreatedAtUtc, n.CreatedByUserId?.ToString(), n.CreatedByUserName)).ToList());

    private static AssetDto MapToDtoFromDetail(AssetDetailDto d) => new(
        d.Id,
        d.AssetNumber,
        d.Name,
        d.SerialNumber,
        d.InternalCode,
        d.Barcode,
        d.RfidTag,
        d.CategoryId,
        d.CategoryName,
        d.AssetTypeId,
        d.AssetTypeName,
        d.Make,
        d.Model,
        d.Year,
        d.OwnershipType,
        d.Status,
        d.Condition,
        d.AssignmentType,
        d.CurrentBranchId,
        d.CurrentBranchName,
        d.CurrentLocationId,
        d.CurrentLocationName,
        d.DepartmentId,
        d.DepartmentName,
        d.TeamId,
        d.TeamName,
        d.CurrentCustodianEmployeeId,
        d.CurrentCustodianEmployeeName,
        d.CurrentAssignedVehicleId,
        d.CurrentAssignedVehiclePlate,
        d.PurchaseCost,
        d.PurchaseDate,
        d.WarrantyExpiryDate,
        d.NextInspectionDueUtc,
        d.NextCalibrationDueUtc,
        d.TotalUsageHours,
        d.IsActive,
        d.CreatedAtUtc);
}

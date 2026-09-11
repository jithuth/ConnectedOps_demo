using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DriveType = ConnectedOps.Domain.Vehicles.DriveType;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public VehicleService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<VehicleListItemDto>> GetVehiclesPagedAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysFromNow = today.AddDays(30);

        var query = _dbContext.Vehicles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        // Filter: SearchTerm
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.VehicleNumber.ToLower().Contains(term) ||
                x.DisplayName.ToLower().Contains(term) ||
                (x.RegistrationNumber != null && x.RegistrationNumber.ToLower().Contains(term)) ||
                (x.VIN != null && x.VIN.ToLower().Contains(term)) ||
                (x.ChassisNumber != null && x.ChassisNumber.ToLower().Contains(term)) ||
                (x.InternalCode != null && x.InternalCode.ToLower().Contains(term)));
        }

        // Filter: Category
        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(x => x.VehicleCategoryId == parameters.CategoryId.Value);
        }

        // Filter: Make
        if (parameters.MakeId.HasValue)
        {
            query = query.Where(x => x.VehicleMakeId == parameters.MakeId.Value);
        }

        // Filter: Model
        if (parameters.ModelId.HasValue)
        {
            query = query.Where(x => x.VehicleModelId == parameters.ModelId.Value);
        }

        // Filter: Branch
        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == parameters.BranchId.Value);
        }

        // Filter: Location
        if (parameters.LocationId.HasValue)
        {
            query = query.Where(x => x.LocationId == parameters.LocationId.Value);
        }

        // Filter: Status
        if (parameters.Status.HasValue)
        {
            query = query.Where(x => x.Status == parameters.Status.Value);
        }

        // Filter: FuelType
        if (parameters.FuelType.HasValue)
        {
            query = query.Where(x => x.FuelType == parameters.FuelType.Value);
        }

        // Filter: OwnershipType
        if (parameters.OwnershipType.HasValue)
        {
            query = query.Where(x => x.OwnershipType == parameters.OwnershipType.Value);
        }

        // Filter: IsActive
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == parameters.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize < 1 ? 20 : (parameters.PageSize > 100 ? 100 : parameters.PageSize);

        var items = await query
            .OrderBy(x => x.VehicleNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Vehicle = x,
                CategoryName = x.VehicleCategory.Name,
                MakeName = x.VehicleMake.Name,
                ModelName = x.VehicleModel.Name,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                LocationName = x.Location != null ? x.Location.Name : null,
                DocCount = x.Documents.Count,
                ExpiringDocCount = x.Documents.Count(d => d.ExpiryDate != null && d.ExpiryDate <= thirtyDaysFromNow)
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new VehicleListItemDto(
            x.Vehicle.Id,
            x.Vehicle.VehicleNumber,
            x.Vehicle.DisplayName,
            x.Vehicle.RegistrationNumber,
            x.Vehicle.VIN,
            x.Vehicle.VehicleCategoryId,
            x.CategoryName,
            x.Vehicle.VehicleMakeId,
            x.MakeName,
            x.Vehicle.VehicleModelId,
            x.ModelName,
            x.Vehicle.ModelYear,
            x.Vehicle.FuelType,
            x.Vehicle.FuelType.ToString(),
            x.Vehicle.TransmissionType,
            x.Vehicle.TransmissionType.ToString(),
            x.Vehicle.OwnershipType,
            x.Vehicle.OwnershipType.ToString(),
            x.Vehicle.Status,
            x.Vehicle.Status.ToString(),
            x.Vehicle.BranchId,
            x.BranchName,
            x.Vehicle.LocationId,
            x.LocationName,
            x.Vehicle.CurrentOdometer,
            x.Vehicle.OdometerUnit,
            x.Vehicle.IsActive,
            x.Vehicle.PrimaryImageObjectKey,
            x.Vehicle.CreatedAtUtc,
            x.DocCount,
            x.ExpiringDocCount)).ToList();

        return new PagedResult<VehicleListItemDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<VehicleDetailDto> GetVehicleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.VehicleCategory)
            .Include(x => x.VehicleMake)
            .Include(x => x.VehicleModel)
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Specification)
            .Include(x => x.Registrations)
            .Include(x => x.OdometerEntries)
            .Include(x => x.Documents)
            .Include(x => x.NotesList)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        return MapToDetailDto(vehicle);
    }

    public async Task<VehicleDetailDto> CreateVehicleAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        var numberExists = await _dbContext.Vehicles
            .AnyAsync(x => x.TenantId == tenantId && x.VehicleNumber == normalizedNumber, cancellationToken);

        if (numberExists)
            throw new ConflictException($"Vehicle number '{normalizedNumber}' already exists.");

        var categoryExists = await _dbContext.VehicleCategories
            .AnyAsync(x => x.Id == request.CategoryId && x.TenantId == tenantId, cancellationToken);
        if (!categoryExists)
            throw new KeyNotFoundException($"Vehicle category '{request.CategoryId}' was not found.");

        var makeExists = await _dbContext.VehicleMakes
            .AnyAsync(x => x.Id == request.MakeId && x.TenantId == tenantId, cancellationToken);
        if (!makeExists)
            throw new KeyNotFoundException($"Vehicle make '{request.MakeId}' was not found.");

        var modelExists = await _dbContext.VehicleModels
            .AnyAsync(x => x.Id == request.ModelId && x.TenantId == tenantId && x.VehicleMakeId == request.MakeId, cancellationToken);
        if (!modelExists)
            throw new KeyNotFoundException($"Vehicle model '{request.ModelId}' was not found for make '{request.MakeId}'.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found.");
        }

        if (request.LocationId.HasValue)
        {
            var locationExists = await _dbContext.Locations
                .AnyAsync(x => x.Id == request.LocationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!locationExists)
                throw new KeyNotFoundException($"Location '{request.LocationId.Value}' was not found.");
        }

        var vehicle = new Vehicle(
            tenantId,
            normalizedNumber,
            request.CategoryId,
            request.MakeId,
            request.ModelId,
            request.DisplayName,
            request.InternalCode,
            request.RegistrationNumber,
            request.VIN,
            request.ChassisNumber,
            request.EngineNumber,
            request.ModelYear,
            request.ManufactureYear,
            request.FuelType,
            request.TransmissionType,
            request.OwnershipType,
            request.BranchId,
            request.LocationId,
            request.InitialOdometer,
            request.OdometerUnit,
            request.Status,
            request.Color,
            request.NumberOfSeats,
            request.GrossVehicleWeight,
            request.PayloadCapacity,
            request.PurchaseDate,
            request.PurchasePrice,
            request.CurrencyCode,
            request.InServiceDate,
            request.Notes);

        if (request.OwnershipType == OwnershipType.Leased)
        {
            vehicle.UpdateOwnership(
                request.OwnershipType,
                request.OwnerName,
                request.LeaseCompany,
                request.LeaseStartDate,
                request.LeaseEndDate,
                request.MonthlyLeaseCost,
                request.PurchaseDate,
                request.PurchasePrice,
                request.CurrencyCode);
        }

        _dbContext.Vehicles.Add(vehicle);

        if (request.InitialOdometer > 0)
        {
            var initialOdometerEntry = new VehicleOdometerEntry(
                tenantId,
                vehicle.Id,
                request.InitialOdometer,
                request.OdometerUnit,
                DateTime.UtcNow,
                OdometerSource.Manual,
                "Initial odometer reading at registration",
                _currentUserContext.UserId);

            _dbContext.VehicleOdometerEntries.Add(initialOdometerEntry);
        }

        if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
        {
            var initialRegistration = new VehicleRegistration(
                tenantId,
                vehicle.Id,
                request.RegistrationNumber.Trim().ToUpperInvariant(),
                registrationDate: DateOnly.FromDateTime(DateTime.UtcNow),
                isCurrent: true,
                notes: "Initial registration");

            _dbContext.VehicleRegistrations.Add(initialRegistration);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Created vehicle {vehicle.VehicleNumber} ({vehicle.DisplayName})"),
            cancellationToken);

        return await GetVehicleByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleDetailDto> UpdateVehicleAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        var numberExists = await _dbContext.Vehicles
            .AnyAsync(x => x.TenantId == tenantId && x.Id != id && x.VehicleNumber == normalizedNumber, cancellationToken);

        if (numberExists)
            throw new ConflictException($"Vehicle number '{normalizedNumber}' already exists.");

        var categoryExists = await _dbContext.VehicleCategories
            .AnyAsync(x => x.Id == request.CategoryId && x.TenantId == tenantId, cancellationToken);
        if (!categoryExists)
            throw new KeyNotFoundException($"Vehicle category '{request.CategoryId}' was not found.");

        var makeExists = await _dbContext.VehicleMakes
            .AnyAsync(x => x.Id == request.MakeId && x.TenantId == tenantId, cancellationToken);
        if (!makeExists)
            throw new KeyNotFoundException($"Vehicle make '{request.MakeId}' was not found.");

        var modelExists = await _dbContext.VehicleModels
            .AnyAsync(x => x.Id == request.ModelId && x.TenantId == tenantId && x.VehicleMakeId == request.MakeId, cancellationToken);
        if (!modelExists)
            throw new KeyNotFoundException($"Vehicle model '{request.ModelId}' was not found for make '{request.MakeId}'.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found.");
        }

        if (request.LocationId.HasValue)
        {
            var locationExists = await _dbContext.Locations
                .AnyAsync(x => x.Id == request.LocationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!locationExists)
                throw new KeyNotFoundException($"Location '{request.LocationId.Value}' was not found.");
        }

        vehicle.UpdateGeneralInfo(
            normalizedNumber,
            request.DisplayName,
            request.Color,
            request.NumberOfSeats,
            request.Notes);

        vehicle.UpdateIdentification(
            request.InternalCode,
            request.RegistrationNumber,
            request.VIN,
            request.ChassisNumber,
            request.EngineNumber);

        vehicle.UpdateClassification(
            request.CategoryId,
            request.MakeId,
            request.ModelId,
            request.ModelYear,
            request.ManufactureYear,
            request.FuelType,
            request.TransmissionType);

        vehicle.UpdateOwnership(
            request.OwnershipType,
            request.OwnerName,
            request.LeaseCompany,
            request.LeaseStartDate,
            request.LeaseEndDate,
            request.MonthlyLeaseCost,
            request.PurchaseDate,
            request.PurchasePrice,
            request.CurrencyCode);

        vehicle.AssignBranch(request.BranchId);
        vehicle.AssignLocation(request.LocationId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Updated vehicle {vehicle.VehicleNumber}"),
            cancellationToken);

        return await GetVehicleByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeVehicleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        var oldStatus = vehicle.Status;
        vehicle.SetStatus(request.NewStatus);

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var note = new VehicleNote(
                tenantId,
                id,
                $"Status changed from {oldStatus} to {request.NewStatus}. Reason: {request.Notes.Trim()}",
                _currentUserContext.UserId,
                _currentUserContext.Email);

            _dbContext.VehicleNotes.Add(note);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.StatusChanged,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Changed status of vehicle {vehicle.VehicleNumber} from {oldStatus} to {request.NewStatus}"),
            cancellationToken);

        return await GetVehicleByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleDetailDto> AssignBranchAsync(
        Guid id,
        AssignVehicleBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found.");
        }

        vehicle.AssignBranch(request.BranchId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Assigned,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Assigned vehicle {vehicle.VehicleNumber} to branch {request.BranchId}"),
            cancellationToken);

        return await GetVehicleByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleDetailDto> AssignLocationAsync(
        Guid id,
        AssignVehicleLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        if (request.LocationId.HasValue)
        {
            var locationExists = await _dbContext.Locations
                .AnyAsync(x => x.Id == request.LocationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!locationExists)
                throw new KeyNotFoundException($"Location '{request.LocationId.Value}' was not found.");
        }

        vehicle.AssignLocation(request.LocationId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Assigned,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Assigned vehicle {vehicle.VehicleNumber} to location {request.LocationId}"),
            cancellationToken);

        return await GetVehicleByIdAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleSpecificationDto> UpsertSpecificationAsync(
        Guid id,
        UpsertVehicleSpecificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .Include(x => x.Specification)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        VehicleSpecification spec;
        if (vehicle.Specification is null)
        {
            spec = new VehicleSpecification(
                tenantId,
                id,
                request.EngineCapacityCc,
                request.EnginePowerKw,
                request.CylinderCount,
                request.FuelTankCapacity,
                request.BatteryVoltage,
                request.LengthMm,
                request.WidthMm,
                request.HeightMm,
                request.GrossVehicleWeightKg,
                request.KerbWeightKg,
                request.PayloadCapacityKg,
                request.AxleCount,
                request.WheelCount,
                request.SeatCount,
                request.BodyType,
                request.DriveType,
                request.EmissionStandard,
                request.TyreSizeFront,
                request.TyreSizeRear);

            _dbContext.VehicleSpecifications.Add(spec);
        }
        else
        {
            spec = vehicle.Specification;
            spec.Update(
                request.EngineCapacityCc,
                request.EnginePowerKw,
                request.CylinderCount,
                request.FuelTankCapacity,
                request.BatteryVoltage,
                request.LengthMm,
                request.WidthMm,
                request.HeightMm,
                request.GrossVehicleWeightKg,
                request.KerbWeightKg,
                request.PayloadCapacityKg,
                request.AxleCount,
                request.WheelCount,
                request.SeatCount,
                request.BodyType,
                request.DriveType,
                request.EmissionStandard,
                request.TyreSizeFront,
                request.TyreSizeRear);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "VehicleSpecification",
                id.ToString(),
                $"Updated technical specification for vehicle {vehicle.VehicleNumber}"),
            cancellationToken);

        return MapSpecificationToDto(spec);
    }

    public async Task<VehicleRegistrationDto> AddRegistrationAsync(
        Guid id,
        CreateVehicleRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .Include(x => x.Registrations)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        // Mark existing current registrations as not current
        var currentRegs = await _dbContext.VehicleRegistrations
            .Where(r => r.TenantId == tenantId && r.VehicleId == id && r.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var reg in currentRegs)
        {
            reg.MarkNotCurrent();
        }

        var newReg = new VehicleRegistration(
            tenantId,
            id,
            request.RegistrationNumber,
            request.RegistrationCountryCode,
            request.RegistrationStateProvince,
            request.RegistrationDate,
            request.ExpiryDate,
            request.IssuingAuthority,
            true,
            request.Notes);

        vehicle.UpdateIdentification(
            vehicle.InternalCode,
            request.RegistrationNumber,
            vehicle.VIN,
            vehicle.ChassisNumber,
            vehicle.EngineNumber);

        _dbContext.VehicleRegistrations.Add(newReg);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "VehicleRegistration",
                newReg.Id.ToString(),
                $"Added registration {newReg.RegistrationNumber} to vehicle {vehicle.VehicleNumber}"),
            cancellationToken);

        return MapRegistrationToDto(newReg);
    }

    public async Task<VehicleNoteDto> AddNoteAsync(
        Guid id,
        CreateVehicleNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        var note = new VehicleNote(
            tenantId,
            id,
            request.NoteText,
            _currentUserContext.UserId,
            _currentUserContext.Email);

        _dbContext.VehicleNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VehicleNoteDto(
            note.Id,
            note.VehicleId,
            note.NoteText,
            note.CreatedByUserId,
            note.CreatedByUserName,
            note.CreatedAtUtc);
    }

    public async Task SetPrimaryImageAsync(
        Guid id,
        string? objectKey,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        vehicle.SetPrimaryImage(objectKey);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVehicleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        vehicle.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Vehicle",
                vehicle.Id.ToString(),
                $"Soft-deleted vehicle {vehicle.VehicleNumber}"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static VehicleDetailDto MapToDetailDto(Vehicle v)
    {
        var currentReg = v.Registrations.FirstOrDefault(r => r.IsCurrent);

        return new VehicleDetailDto(
            v.Id,
            v.TenantId,
            v.VehicleNumber,
            v.DisplayName,
            v.InternalCode,
            v.RegistrationNumber,
            v.VIN,
            v.ChassisNumber,
            v.EngineNumber,
            v.VehicleCategoryId,
            v.VehicleCategory?.Name ?? string.Empty,
            v.VehicleMakeId,
            v.VehicleMake?.Name ?? string.Empty,
            v.VehicleModelId,
            v.VehicleModel?.Name ?? string.Empty,
            v.ModelYear,
            v.ManufactureYear,
            v.FuelType,
            v.FuelType.ToString(),
            v.TransmissionType,
            v.TransmissionType.ToString(),
            v.Color,
            v.NumberOfSeats,
            v.GrossVehicleWeight,
            v.PayloadCapacity,
            v.OwnershipType,
            v.OwnershipType.ToString(),
            v.OwnerName,
            v.LeaseCompany,
            v.LeaseStartDate,
            v.LeaseEndDate,
            v.MonthlyLeaseCost,
            v.PurchaseDate,
            v.PurchasePrice,
            v.CurrencyCode,
            v.BranchId,
            v.Branch?.Name,
            v.LocationId,
            v.Location?.Name,
            v.Status,
            v.Status.ToString(),
            v.InServiceDate,
            v.OutOfServiceDate,
            v.IsActive,
            v.CurrentOdometer,
            v.OdometerUnit,
            v.PrimaryImageObjectKey,
            v.Notes,
            v.CreatedAtUtc,
            v.UpdatedAtUtc,
            v.Specification != null ? MapSpecificationToDto(v.Specification) : null,
            currentReg != null ? MapRegistrationToDto(currentReg) : null,
            v.Registrations.OrderByDescending(r => r.IsCurrent).ThenByDescending(r => r.RegistrationDate).Select(MapRegistrationToDto).ToList(),
            v.OdometerEntries.Select(o => new VehicleOdometerEntryDto(
                o.Id,
                o.VehicleId,
                o.Reading,
                o.Unit,
                o.Unit.ToString(),
                o.ReadingDateUtc,
                o.Source,
                o.Source.ToString(),
                o.Notes,
                o.RecordedByUserId,
                o.CreatedAtUtc)).ToList(),
            v.Documents.Select(d => new VehicleDocumentDto(
                d.Id,
                d.VehicleId,
                d.DocumentType,
                d.DocumentType.ToString(),
                d.Title,
                d.DocumentNumber,
                d.IssuingAuthority,
                d.IssueDate,
                d.ExpiryDate,
                d.FileObjectKey ?? string.Empty,
                d.FileName ?? string.Empty,
                d.ContentType ?? string.Empty,
                d.FileSizeBytes,
                d.IsExpired(),
                d.IsExpiringSoon(),
                d.Notes,
                d.CreatedAtUtc)).ToList(),
            v.NotesList.Select(n => new VehicleNoteDto(
                n.Id,
                n.VehicleId,
                n.NoteText,
                n.CreatedByUserId,
                n.CreatedByUserName,
                n.CreatedAtUtc)).ToList());
    }

    private static VehicleSpecificationDto MapSpecificationToDto(VehicleSpecification s) =>
        new(
            s.VehicleId,
            s.EngineCapacityCc,
            s.EnginePowerKw,
            s.CylinderCount,
            s.FuelTankCapacity,
            s.BatteryVoltage,
            s.LengthMm,
            s.WidthMm,
            s.HeightMm,
            s.GrossVehicleWeightKg,
            s.KerbWeightKg,
            s.PayloadCapacityKg,
            s.AxleCount,
            s.WheelCount,
            s.SeatCount,
            s.BodyType,
            s.DriveType,
            s.DriveType.ToString(),
            s.EmissionStandard,
            s.TyreSizeFront,
            s.TyreSizeRear);

    private static VehicleRegistrationDto MapRegistrationToDto(VehicleRegistration r) =>
        new(
            r.Id,
            r.VehicleId,
            r.RegistrationNumber,
            r.RegistrationCountryCode,
            r.RegistrationStateProvince,
            r.RegistrationDate,
            r.ExpiryDate,
            r.IssuingAuthority,
            r.IsCurrent,
            r.Notes,
            r.IsExpired());
}

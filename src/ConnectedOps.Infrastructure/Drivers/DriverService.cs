using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverService : IDriverService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DriverService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<DriverListItemDto>> GetDriversPagedAsync(
        DriverQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var thirtyDaysOut = today.AddDays(30);

        var query = _dbContext.Drivers
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId);

        // SearchTerm filter
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.DriverNumber.ToLower().Contains(term) ||
                d.DisplayName.ToLower().Contains(term) ||
                d.FirstName.ToLower().Contains(term) ||
                d.LastName.ToLower().Contains(term) ||
                (d.Email != null && d.Email.ToLower().Contains(term)) ||
                d.Phone.ToLower().Contains(term) ||
                (d.PrimaryLicenseNumber != null && d.PrimaryLicenseNumber.ToLower().Contains(term)));
        }

        // Status filter
        if (parameters.Status.HasValue)
        {
            query = query.Where(d => d.Status == parameters.Status.Value);
        }

        // DriverType filter
        if (parameters.DriverType.HasValue)
        {
            query = query.Where(d => d.DriverType == parameters.DriverType.Value);
        }

        // Branch filter
        if (parameters.BranchId.HasValue)
        {
            query = query.Where(d => d.BranchId == parameters.BranchId.Value);
        }

        // Department filter
        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId == parameters.DepartmentId.Value);
        }

        // IsActive filter
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == parameters.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .OrderBy(d => d.DriverNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                Driver = d,
                BranchName = d.Branch != null ? d.Branch.Name : null,
                DepartmentName = d.Department != null ? d.Department.Name : null,
                PrimaryLicense = d.Licenses.FirstOrDefault(l => l.IsPrimary && l.IsActive) ?? d.Licenses.FirstOrDefault(l => l.IsActive),
                ActiveAssignment = d.VehicleAssignments
                    .Where(a => a.IsActive && a.AssignedFromUtc <= now && (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
                    .OrderByDescending(a => a.IsPrimary)
                    .Select(a => new
                    {
                        a.VehicleId,
                        a.Vehicle.VehicleNumber,
                        a.Vehicle.DisplayName
                    })
                    .FirstOrDefault(),
                DocCount = d.Documents.Count(x => !x.IsDeleted),
                ExpiringDocCount = d.Documents.Count(x => !x.IsDeleted && x.ExpiryDate.HasValue && x.ExpiryDate.Value <= thirtyDaysOut),
                CertCount = d.Certifications.Count(x => !x.IsDeleted),
                ExpiringCertCount = d.Certifications.Count(x => !x.IsDeleted && x.ExpiryDate.HasValue && x.ExpiryDate.Value <= thirtyDaysOut)
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x =>
        {
            var isExpired = x.PrimaryLicense != null && x.PrimaryLicense.ExpiryDate.HasValue && x.PrimaryLicense.ExpiryDate.Value < today;
            var isExpiringSoon = x.PrimaryLicense != null && x.PrimaryLicense.ExpiryDate.HasValue && !isExpired && x.PrimaryLicense.ExpiryDate.Value <= thirtyDaysOut;

            var availabilityStatus = x.Driver.Status != DriverStatus.Active
                ? DriverAvailabilityStatus.Unavailable
                : (x.ActiveAssignment != null ? DriverAvailabilityStatus.Assigned : DriverAvailabilityStatus.Available);

            return new DriverListItemDto(
                x.Driver.Id,
                x.Driver.DriverNumber,
                x.Driver.DisplayName,
                x.Driver.FirstName,
                x.Driver.LastName,
                x.Driver.Phone,
                x.Driver.Email,
                x.Driver.DriverType,
                x.Driver.DriverType.ToString(),
                x.Driver.Status,
                x.Driver.Status.ToString(),
                availabilityStatus,
                availabilityStatus.ToString(),
                x.Driver.BranchId,
                x.BranchName,
                x.Driver.DepartmentId,
                x.DepartmentName,
                x.PrimaryLicense?.LicenseNumber ?? x.Driver.PrimaryLicenseNumber,
                x.PrimaryLicense?.ExpiryDate,
                isExpired,
                isExpiringSoon,
                x.ActiveAssignment?.VehicleId,
                x.ActiveAssignment?.VehicleNumber,
                x.ActiveAssignment?.DisplayName,
                x.Driver.IsActive,
                x.Driver.CreatedAtUtc,
                x.DocCount,
                x.ExpiringDocCount,
                x.CertCount,
                x.ExpiringCertCount);
        }).ToList();

        // Optional post-query filtering for assignment/license flags
        if (parameters.HasActiveAssignment.HasValue)
        {
            dtos = dtos.Where(d => (d.CurrentVehicleId != null) == parameters.HasActiveAssignment.Value).ToList();
        }
        if (parameters.IsLicenseExpired.HasValue)
        {
            dtos = dtos.Where(d => d.IsLicenseExpired == parameters.IsLicenseExpired.Value).ToList();
        }
        if (parameters.IsLicenseExpiringSoon.HasValue)
        {
            dtos = dtos.Where(d => d.IsLicenseExpiringSoon == parameters.IsLicenseExpiringSoon.Value).ToList();
        }

        return new PagedResult<DriverListItemDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<DriverDetailDto> GetDriverByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;

        var driver = await _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.Employee)
            .Include(d => d.Branch)
            .Include(d => d.Department)
            .Include(d => d.Licenses)
                .ThenInclude(l => l.Categories)
            .Include(d => d.Certifications)
            .Include(d => d.Documents)
            .Include(d => d.VehicleAssignments)
                .ThenInclude(a => a.Vehicle)
            .Include(d => d.EmergencyContacts)
            .Include(d => d.NotesList)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        return MapToDetailDto(driver, now);
    }

    public async Task<DriverDetailDto> CreateDriverAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedNumber = request.DriverNumber.Trim().ToUpperInvariant();

        var numberExists = await _dbContext.Drivers
            .AnyAsync(d => d.TenantId == tenantId && d.DriverNumber == normalizedNumber, cancellationToken);

        if (numberExists)
            throw new ConflictException($"Driver number '{normalizedNumber}' already exists.");

        if (request.EmployeeId.HasValue)
        {
            var employeeExists = await _dbContext.Employees
                .AnyAsync(e => e.Id == request.EmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!employeeExists)
                throw new KeyNotFoundException($"Employee '{request.EmployeeId.Value}' was not found.");

            var alreadyLinked = await _dbContext.Drivers
                .AnyAsync(d => d.TenantId == tenantId && d.EmployeeId == request.EmployeeId.Value, cancellationToken);
            if (alreadyLinked)
                throw new ConflictException($"Employee '{request.EmployeeId.Value}' is already linked to another driver.");
        }

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(b => b.Id == request.BranchId.Value && b.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found.");
        }

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId.Value && d.TenantId == tenantId, cancellationToken);
            if (!deptExists)
                throw new KeyNotFoundException($"Department '{request.DepartmentId.Value}' was not found.");
        }

        var driver = new Driver(
            tenantId,
            normalizedNumber,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DriverType,
            request.MiddleName,
            request.DisplayName,
            request.Email,
            request.AlternatePhone,
            request.Status,
            request.EmployeeId,
            request.BranchId,
            request.DepartmentId,
            request.HireDate,
            request.StartDate,
            request.EndDate,
            request.DateOfBirth,
            request.NationalityCode,
            request.InitialLicenseNumber,
            request.PreferredLanguage,
            request.Notes);

        _dbContext.Drivers.Add(driver);

        // Optional Initial License creation
        if (!string.IsNullOrWhiteSpace(request.InitialLicenseNumber) && !string.IsNullOrWhiteSpace(request.InitialLicenseCountryCode))
        {
            var initialLicense = new DriverLicense(
                tenantId,
                driver.Id,
                request.InitialLicenseNumber,
                request.InitialLicenseCountryCode,
                request.InitialLicenseAuthority,
                null,
                request.InitialLicenseExpiryDate,
                isPrimary: true);

            if (!string.IsNullOrWhiteSpace(request.InitialLicenseCategoryCode))
            {
                var cat = new DriverLicenseCategory(tenantId, initialLicense.Id, request.InitialLicenseCategoryCode);
                _dbContext.DriverLicenseCategories.Add(cat);
            }

            _dbContext.DriverLicenses.Add(initialLicense);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Driver",
                driver.Id.ToString(),
                $"Created driver {driver.DriverNumber} - {driver.DisplayName}"),
            cancellationToken);

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverDetailDto> UpdateDriverAsync(
        Guid id,
        UpdateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        driver.UpdateGeneralInfo(
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.DisplayName,
            request.Email,
            request.Phone,
            request.AlternatePhone,
            request.DriverType,
            request.DateOfBirth,
            request.NationalityCode,
            request.PreferredLanguage,
            request.Notes);

        driver.UpdateDates(
            request.HireDate,
            request.StartDate,
            request.EndDate);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Driver",
                driver.Id.ToString(),
                $"Updated driver {driver.DriverNumber}"),
            cancellationToken);

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeDriverStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        var oldStatus = driver.Status;
        driver.SetStatus(request.NewStatus);

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var note = new DriverNote(
                tenantId,
                id,
                $"Status changed from {oldStatus} to {request.NewStatus}. Reason: {request.Notes.Trim()}",
                _currentUserContext.UserId,
                _currentUserContext.Email);

            _dbContext.DriverNotes.Add(note);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DriverStatusChanged,
                "Driver",
                driver.Id.ToString(),
                $"Changed status of driver {driver.DriverNumber} from {oldStatus} to {request.NewStatus}"),
            cancellationToken);

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverDetailDto> LinkEmployeeAsync(
        Guid id,
        LinkDriverEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        if (request.EmployeeId.HasValue)
        {
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (employee is null)
                throw new KeyNotFoundException($"Employee '{request.EmployeeId.Value}' was not found.");

            var alreadyLinked = await _dbContext.Drivers
                .AnyAsync(d => d.TenantId == tenantId && d.EmployeeId == request.EmployeeId.Value && d.Id != id, cancellationToken);
            if (alreadyLinked)
                throw new ConflictException($"Employee '{request.EmployeeId.Value}' is already linked to another driver.");

            driver.LinkEmployee(request.EmployeeId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditLogService.WriteAsync(
                new CreateAuditLogRequest(
                    AuditAction.DriverEmployeeLinked,
                    "Driver",
                    driver.Id.ToString(),
                    $"Linked employee {employee.EmployeeNumber} ({employee.FullName}) to driver {driver.DriverNumber}"),
                cancellationToken);
        }
        else
        {
            driver.LinkEmployee(null);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditLogService.WriteAsync(
                new CreateAuditLogRequest(
                    AuditAction.DriverEmployeeUnlinked,
                    "Driver",
                    driver.Id.ToString(),
                    $"Unlinked employee from driver {driver.DriverNumber}"),
                cancellationToken);
        }

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverDetailDto> AssignBranchAsync(
        Guid id,
        AssignDriverBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(b => b.Id == request.BranchId.Value && b.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId.Value}' was not found.");
        }

        driver.AssignBranch(request.BranchId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Driver",
                driver.Id.ToString(),
                $"Updated branch assignment for driver {driver.DriverNumber}"),
            cancellationToken);

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverDetailDto> AssignDepartmentAsync(
        Guid id,
        AssignDriverDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId.Value && d.TenantId == tenantId, cancellationToken);
            if (!deptExists)
                throw new KeyNotFoundException($"Department '{request.DepartmentId.Value}' was not found.");
        }

        driver.AssignDepartment(request.DepartmentId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Driver",
                driver.Id.ToString(),
                $"Updated department assignment for driver {driver.DriverNumber}"),
            cancellationToken);

        return await GetDriverByIdAsync(driver.Id, cancellationToken);
    }

    public async Task<DriverEmergencyContactDto> AddEmergencyContactAsync(
        Guid driverId,
        CreateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        if (request.IsPrimary)
        {
            var existingContacts = await _dbContext.DriverEmergencyContacts
                .Where(c => c.TenantId == tenantId && c.DriverId == driverId && c.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingContacts)
            {
                existing.SetPrimary(false);
            }
        }

        var contact = new DriverEmergencyContact(
            tenantId,
            driverId,
            request.Name,
            request.Relationship,
            request.Phone,
            request.AlternatePhone,
            request.IsPrimary);

        _dbContext.DriverEmergencyContacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DriverEmergencyContactDto(
            contact.Id,
            contact.DriverId,
            contact.Name,
            contact.Relationship,
            contact.Phone,
            contact.AlternatePhone,
            contact.IsPrimary);
    }

    public async Task<DriverEmergencyContactDto> UpdateEmergencyContactAsync(
        Guid driverId,
        Guid contactId,
        UpdateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var contact = await _dbContext.DriverEmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (contact is null)
            throw new KeyNotFoundException($"Emergency contact '{contactId}' was not found.");

        if (request.IsPrimary && !contact.IsPrimary)
        {
            var otherContacts = await _dbContext.DriverEmergencyContacts
                .Where(c => c.TenantId == tenantId && c.DriverId == driverId && c.Id != contactId && c.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var other in otherContacts)
            {
                other.SetPrimary(false);
            }
        }

        contact.Update(
            request.Name,
            request.Relationship,
            request.Phone,
            request.AlternatePhone,
            request.IsPrimary);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DriverEmergencyContactDto(
            contact.Id,
            contact.DriverId,
            contact.Name,
            contact.Relationship,
            contact.Phone,
            contact.AlternatePhone,
            contact.IsPrimary);
    }

    public async Task DeleteEmergencyContactAsync(
        Guid driverId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var contact = await _dbContext.DriverEmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (contact is null)
            throw new KeyNotFoundException($"Emergency contact '{contactId}' was not found.");

        contact.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DriverNoteDto> AddNoteAsync(
        Guid driverId,
        CreateDriverNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var note = new DriverNote(
            tenantId,
            driverId,
            request.NoteText,
            _currentUserContext.UserId,
            _currentUserContext.Email);

        _dbContext.DriverNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DriverNoteDto(
            note.Id,
            note.DriverId,
            note.NoteText,
            note.CreatedByUserId,
            note.CreatedByUserName,
            note.CreatedAtUtc);
    }

    public async Task SetProfileImageAsync(
        Guid driverId,
        string? objectKey,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        driver.SetProfileImage(objectKey);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDriverAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{id}' was not found.");

        driver.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Driver",
                driver.Id.ToString(),
                $"Soft-deleted driver {driver.DriverNumber}"),
            cancellationToken);
    }

    private static DriverDetailDto MapToDetailDto(Driver d, DateTime now)
    {
        var activeAssignment = d.VehicleAssignments
            .Where(a => a.IsActive && a.AssignedFromUtc <= now && (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.AssignedFromUtc)
            .FirstOrDefault();

        var availability = d.Status != DriverStatus.Active
            ? DriverAvailabilityStatus.Unavailable
            : (activeAssignment != null ? DriverAvailabilityStatus.Assigned : DriverAvailabilityStatus.Available);

        return new DriverDetailDto(
            d.Id,
            d.TenantId,
            d.DriverNumber,
            d.EmployeeId,
            d.Employee?.EmployeeNumber,
            d.Employee?.FullName,
            d.FirstName,
            d.MiddleName,
            d.LastName,
            d.DisplayName,
            d.Email,
            d.Phone,
            d.AlternatePhone,
            d.DriverType,
            d.DriverType.ToString(),
            d.Status,
            d.Status.ToString(),
            availability,
            availability.ToString(),
            d.BranchId,
            d.Branch?.Name,
            d.DepartmentId,
            d.Department?.Name,
            d.HireDate,
            d.StartDate,
            d.EndDate,
            d.DateOfBirth,
            d.NationalityCode,
            d.PrimaryLicenseNumber,
            d.PreferredLanguage,
            d.ProfileImageObjectKey,
            d.Notes,
            d.IsActive,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            activeAssignment is null ? null : MapAssignmentDto(activeAssignment),
            d.Licenses.OrderByDescending(l => l.IsPrimary).ThenByDescending(l => l.ExpiryDate).Select(MapLicenseDto).ToList(),
            d.Certifications.OrderByDescending(c => c.ExpiryDate).Select(MapCertificationDto).ToList(),
            d.Documents.OrderByDescending(doc => doc.ExpiryDate).Select(MapDocumentDto).ToList(),
            d.VehicleAssignments.OrderByDescending(a => a.AssignedFromUtc).Select(MapAssignmentDto).ToList(),
            d.EmergencyContacts.OrderByDescending(c => c.IsPrimary).Select(c => new DriverEmergencyContactDto(
                c.Id, c.DriverId, c.Name, c.Relationship, c.Phone, c.AlternatePhone, c.IsPrimary)).ToList(),
            d.NotesList.OrderByDescending(n => n.CreatedAtUtc).Select(n => new DriverNoteDto(
                n.Id, n.DriverId, n.NoteText, n.CreatedByUserId, n.CreatedByUserName, n.CreatedAtUtc)).ToList());
    }

    private static DriverVehicleAssignmentDto MapAssignmentDto(DriverVehicleAssignment a) =>
        new(
            a.Id,
            a.TenantId,
            a.DriverId,
            a.Driver?.DriverNumber ?? string.Empty,
            a.Driver?.DisplayName ?? string.Empty,
            a.VehicleId,
            a.Vehicle?.VehicleNumber ?? string.Empty,
            a.Vehicle?.DisplayName ?? string.Empty,
            a.Vehicle?.RegistrationNumber,
            a.AssignmentType,
            a.AssignmentType.ToString(),
            a.AssignedFromUtc,
            a.AssignedToUtc,
            a.IsPrimary,
            a.IsActive,
            a.AssignedByUserId,
            a.EndedByUserId,
            a.Reason,
            a.Notes,
            a.CreatedAtUtc);

    private static DriverLicenseDto MapLicenseDto(DriverLicense l) =>
        new(
            l.Id,
            l.DriverId,
            l.LicenseNumber,
            l.LicenseCountryCode,
            l.IssuingAuthority,
            l.IssueDate,
            l.ExpiryDate,
            l.IsPrimary,
            l.IsActive,
            l.IsExpired(),
            l.IsExpiringSoon(),
            l.Notes,
            l.Categories.Select(c => new DriverLicenseCategoryDto(
                c.Id, c.DriverLicenseId, c.CategoryCode, c.Description, c.ValidFrom, c.ValidTo, c.IsActive)).ToList());

    private static DriverCertificationDto MapCertificationDto(DriverCertification c) =>
        new(
            c.Id,
            c.DriverId,
            c.CertificationType,
            c.CertificationType.ToString(),
            c.Title,
            c.CertificateNumber,
            c.IssuedBy,
            c.IssueDate,
            c.ExpiryDate,
            c.IsActive,
            c.IsExpired(),
            c.IsExpiringSoon(),
            c.FileObjectKey,
            c.Notes);

    private static DriverDocumentDto MapDocumentDto(DriverDocument d) =>
        new(
            d.Id,
            d.DriverId,
            d.DocumentType,
            d.DocumentType.ToString(),
            d.Title,
            d.DocumentNumber,
            d.IssueDate,
            d.ExpiryDate,
            d.IssuingAuthority,
            d.FileObjectKey,
            d.FileName,
            d.ContentType,
            d.FileSizeBytes,
            d.IsActive,
            d.IsExpired(),
            d.IsExpiringSoon(),
            d.Notes,
            d.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}

using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Inspections;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Inspections;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Inspections;

public sealed class DvirService : IDvirService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DvirService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        if (!_currentUserContext.TenantId.HasValue || _currentUserContext.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return _currentUserContext.TenantId.Value;
    }

    public async Task<DvirDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;

        var inspections = await _dbContext.DvirInspections
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Items)
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var todayInspections = inspections.Where(d => d.InspectedAtUtc >= today).ToList();
        var groundedCount = inspections.Count(d => d.Status == DvirStatus.VehicleGrounded);
        var activeDefects = inspections
            .Where(d => d.Status == DvirStatus.DefectsReported || d.Status == DvirStatus.VehicleGrounded)
            .SelectMany(d => d.Items)
            .Count(i => !i.IsPassed);

        var certifiedToday = inspections.Count(d => d.Status == DvirStatus.CertifiedSafe && d.CertifiedAtUtc >= today);

        return new DvirDashboardDto(
            TotalInspectionsToday: todayInspections.Count,
            GroundedVehiclesCount: groundedCount,
            ActiveDefectsCount: activeDefects,
            CertifiedTodayCount: certifiedToday,
            RecentInspections: inspections.OrderByDescending(d => d.InspectedAtUtc).Take(10).Select(MapToDvirDto).ToList());
    }

    public async Task<PagedResult<DvirInspectionDto>> GetInspectionsPagedAsync(DvirFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.DvirInspections
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Items)
            .Where(d => d.TenantId == tenantId);

        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status.Value);
        if (request.InspectionType.HasValue)
            query = query.Where(d => d.InspectionType == request.InspectionType.Value);
        if (request.VehicleId.HasValue)
            query = query.Where(d => d.VehicleId == request.VehicleId.Value);
        if (request.DriverId.HasValue)
            query = query.Where(d => d.DriverId == request.DriverId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.InspectionNumber.ToLower().Contains(term) ||
                                     d.Vehicle.RegistrationNumber!.ToLower().Contains(term) ||
                                     d.Driver.FirstName.ToLower().Contains(term) ||
                                     d.Driver.LastName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(d => d.InspectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DvirInspectionDto>(
            items.Select(MapToDvirDto).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<DvirInspectionDto?> GetInspectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var inspection = await _dbContext.DvirInspections
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        return inspection == null ? null : MapToDvirDto(inspection);
    }

    public async Task<DvirInspectionDto> CreateInspectionAsync(CreateDvirRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle == null)
            throw new KeyNotFoundException($"Vehicle with ID '{request.VehicleId}' was not found.");

        var inspectionNumber = $"DVIR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        var inspection = new DvirInspection(
            tenantId: tenantId,
            inspectionNumber: inspectionNumber,
            vehicleId: request.VehicleId,
            driverId: request.DriverId,
            inspectionType: request.InspectionType,
            odometer: request.Odometer,
            locationName: request.LocationName,
            driverSignatureData: request.DriverSignatureData,
            remarks: request.Remarks,
            createdByUserId: _currentUserContext.UserId)
        {
            Vehicle = vehicle
        };

        foreach (var item in request.Items)
        {
            inspection.AddItemCheck(new DvirItemCheck(
                tenantId: tenantId,
                dvirInspectionId: inspection.Id,
                category: item.Category,
                itemName: item.ItemName,
                isPassed: item.IsPassed,
                severity: item.Severity,
                defectDescription: item.DefectDescription));
        }

        _dbContext.DvirInspections.Add(inspection);

        // Ground vehicle if critical defect is discovered during DVIR
        if (inspection.HasCriticalDefect)
        {
            vehicle.SetStatus(VehicleStatus.UnderMaintenance);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetInspectionByIdAsync(inspection.Id, cancellationToken))!;
    }

    public async Task<DvirInspectionDto> CertifyByMechanicAsync(Guid id, SignOffDvirRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var inspection = await _dbContext.DvirInspections
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);

        if (inspection == null)
            throw new KeyNotFoundException($"DVIR Inspection with ID '{id}' was not found.");

        inspection.CertifySafeByMechanic(
            mechanicName: request.MechanicName,
            notes: request.MechanicNotes,
            signatureData: request.MechanicSignatureData,
            userId: _currentUserContext.UserId ?? Guid.Empty);

        // Restore vehicle to InService status if it was grounded
        if (inspection.Vehicle.Status == VehicleStatus.UnderMaintenance)
        {
            inspection.Vehicle.SetStatus(VehicleStatus.InService);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDvirDto(inspection);
    }

    private static DvirInspectionDto MapToDvirDto(DvirInspection d) =>
        new(d.Id,
            d.InspectionNumber,
            d.VehicleId,
            d.Vehicle?.RegistrationNumber ?? d.Vehicle?.VehicleNumber ?? string.Empty,
            $"{d.Vehicle?.VehicleMake?.Name} {d.Vehicle?.VehicleModel?.Name}".Trim(),
            d.DriverId,
            d.Driver?.DisplayName ?? string.Empty,
            d.InspectionType,
            d.InspectionType.ToString(),
            d.Status,
            d.Status.ToString(),
            d.Odometer,
            d.LocationName,
            d.InspectedAtUtc,
            d.Remarks,
            d.DriverSignatureData,
            d.MechanicName,
            d.MechanicNotes,
            d.CertifiedAtUtc,
            d.Items.Select(i => new DvirItemCheckDto(
                i.Id,
                i.Category,
                i.ItemName,
                i.IsPassed,
                i.Severity,
                i.Severity?.ToString(),
                i.DefectDescription)).ToList());
}

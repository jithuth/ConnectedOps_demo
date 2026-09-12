using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class SafetyIncidentService : ISafetyIncidentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IIncidentNumberGenerator _numberGenerator;

    public SafetyIncidentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IIncidentNumberGenerator numberGenerator)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _numberGenerator = numberGenerator;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<PagedResult<SafetyIncidentDto>> GetPagedAsync(SafetyIncidentFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.SafetyIncidents
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Investigation)
            .Include(x => x.Participants)
            .Include(x => x.Vehicles)
            .Include(x => x.Assets)
            .Include(x => x.Evidence)
            .Include(x => x.CorrectiveActions)
            .Where(x => x.TenantId == tenantId);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Severity.HasValue)
            query = query.Where(x => x.Severity == filter.Severity.Value);

        if (filter.IncidentType.HasValue)
            query = query.Where(x => x.IncidentType == filter.IncidentType.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(x => x.BranchId == filter.BranchId.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(x => x.Vehicles.Any(v => v.VehicleId == filter.VehicleId.Value));

        if (filter.DriverId.HasValue)
            query = query.Where(x => x.Participants.Any(p => p.DriverId == filter.DriverId.Value));

        if (filter.AssetId.HasValue)
            query = query.Where(x => x.Assets.Any(a => a.AssetId == filter.AssetId.Value));

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.IncidentNumber.ToLower().Contains(term) ||
                x.Title.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                (x.Branch != null && x.Branch.Name.ToLower().Contains(term)) ||
                (x.Location != null && x.Location.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToSummaryDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<SafetyIncidentDto>(items, totalCount, page, pageSize);
    }

    public async Task<SafetyIncidentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Investigation)
                .ThenInclude(i => i!.InvestigatorEmployee)
            .Include(x => x.Participants)
                .ThenInclude(p => p.Driver)
            .Include(x => x.Participants)
                .ThenInclude(p => p.Employee)
            .Include(x => x.Vehicles)
                .ThenInclude(v => v.Vehicle)
            .Include(x => x.Assets)
                .ThenInclude(a => a.Asset)
            .Include(x => x.Evidence)
            .Include(x => x.CorrectiveActions)
                .ThenInclude(c => c.AssignedEmployee)
            .Include(x => x.Violations)
                .ThenInclude(v => v.Driver)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDetailDto(entity);
    }

    public async Task<SafetyIncidentDto> CreateAsync(CreateSafetyIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var incidentNumber = await _numberGenerator.GenerateIncidentNumberAsync(tenantId, cancellationToken);

        var incident = new SafetyIncident(
            tenantId,
            incidentNumber,
            request.IncidentType,
            request.Severity,
            request.OccurredAtUtc,
            request.Title,
            request.Description,
            reportedAtUtc: DateTime.UtcNow,
            branchId: request.BranchId,
            locationId: request.LocationId,
            latitude: request.Latitude,
            longitude: request.Longitude,
            immediateActionTaken: request.ImmediateActionTaken,
            reportedByUserId: _currentUserContext.UserId,
            investigationRequired: request.InvestigationRequired,
            createdByUserId: _currentUserContext.UserId);

        // Add primary vehicle if provided
        if (request.PrimaryVehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles.AnyAsync(v => v.Id == request.PrimaryVehicleId.Value && v.TenantId == tenantId, cancellationToken);
            if (vehicleExists)
            {
                var incVehicle = new SafetyIncidentVehicle(
                    tenantId,
                    incident.Id,
                    request.PrimaryVehicleId.Value,
                    damageReported: false,
                    isPrimaryVehicle: true,
                    createdByUserId: _currentUserContext.UserId);
                incident.AddVehicle(incVehicle);
            }
        }

        // Add primary driver if provided
        if (request.PrimaryDriverId.HasValue)
        {
            var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == request.PrimaryDriverId.Value && d.TenantId == tenantId, cancellationToken);
            if (driver != null)
            {
                var participant = new SafetyIncidentParticipant(
                    tenantId,
                    incident.Id,
                    SafetyParticipantType.Driver,
                    "Primary Driver",
                    driverId: driver.Id,
                    name: driver.DisplayName,
                    createdByUserId: _currentUserContext.UserId);
                incident.AddParticipant(participant);
            }
        }

        // Add initial evidence if provided
        if (!string.IsNullOrWhiteSpace(request.InitialEvidenceKey) && !string.IsNullOrWhiteSpace(request.InitialEvidenceFileName))
        {
            var evidence = new SafetyIncidentEvidence(
                tenantId,
                incident.Id,
                SafetyEvidenceType.Photo,
                request.InitialEvidenceTitle ?? "Initial Evidence",
                request.InitialEvidenceKey,
                request.InitialEvidenceFileName,
                "image/jpeg",
                0,
                capturedAtUtc: request.OccurredAtUtc,
                uploadedAtUtc: DateTime.UtcNow,
                uploadedByUserId: _currentUserContext.UserId);
            incident.AddEvidence(evidence);
        }

        _dbContext.SafetyIncidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentCreated,
            nameof(SafetyIncident),
            incident.Id.ToString(),
            $"Created safety incident {incident.IncidentNumber}: {incident.Title} ({incident.Severity})",
            null,
            incident));

        return (await GetSummaryDtoByIdAsync(incident.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentDto> UpdateAsync(Guid id, UpdateSafetyIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyIncident), id.ToString());

        entity.Update(
            request.IncidentType,
            request.Severity,
            request.OccurredAtUtc,
            request.Title,
            request.Description,
            request.BranchId,
            request.LocationId,
            request.Latitude,
            request.Longitude,
            request.ImmediateActionTaken,
            request.InvestigationRequired,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentUpdated,
            nameof(SafetyIncident),
            entity.Id.ToString(),
            $"Updated safety incident {entity.IncidentNumber}",
            null,
            entity));

        return (await GetSummaryDtoByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentDto> StartInvestigationAsync(Guid id, StartSafetyInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .Include(x => x.Investigation)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyIncident), id.ToString());

        if (request.InvestigatorEmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees.AnyAsync(e => e.Id == request.InvestigatorEmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!empExists)
                throw new NotFoundException(nameof(Employee), request.InvestigatorEmployeeId.Value.ToString());
        }

        var wasNull = entity.Investigation == null;
        entity.StartInvestigation(request.InvestigatorEmployeeId, _currentUserContext.UserId);
        if (wasNull && entity.Investigation != null)
        {
            _dbContext.SafetyIncidentInvestigations.Add(entity.Investigation);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyInvestigationStarted,
            nameof(SafetyIncidentInvestigation),
            entity.Investigation?.Id.ToString(),
            $"Started safety investigation for incident {entity.IncidentNumber}"));

        return (await GetSummaryDtoByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentDto> CompleteInvestigationAsync(Guid id, CompleteSafetyInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .Include(x => x.Investigation)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyIncident), id.ToString());

        if (entity.Investigation == null)
        {
            entity.StartInvestigation(startedByUserId: _currentUserContext.UserId);
            if (entity.Investigation != null)
            {
                _dbContext.SafetyIncidentInvestigations.Add(entity.Investigation);
            }
        }

        entity.Investigation!.Complete(
            request.Summary,
            request.RootCause,
            request.RootCauseDescription,
            request.ContributingFactors,
            request.Recommendation,
            _currentUserContext.UserId ?? Guid.Empty);

        entity.SetStatus(SafetyIncidentStatus.CorrectiveActionPending, _currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyInvestigationCompleted,
            nameof(SafetyIncidentInvestigation),
            entity.Investigation.Id.ToString(),
            $"Completed safety investigation for incident {entity.IncidentNumber} (Root Cause: {request.RootCause})"));

        return (await GetSummaryDtoByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentDto> CloseAsync(Guid id, CloseSafetyIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyIncident), id.ToString());

        var closerId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");
        entity.Close(closerId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentClosed,
            nameof(SafetyIncident),
            entity.Id.ToString(),
            $"Closed safety incident {entity.IncidentNumber}"));

        return (await GetSummaryDtoByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyIncident), id.ToString());

        entity.Cancel(_currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentCancelled,
            nameof(SafetyIncident),
            entity.Id.ToString(),
            $"Cancelled safety incident {entity.IncidentNumber}"));

        return (await GetSummaryDtoByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyIncidentParticipantDto> AddParticipantAsync(Guid incidentId, AddIncidentParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var incident = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && x.TenantId == tenantId, cancellationToken);

        if (incident == null)
            throw new NotFoundException(nameof(SafetyIncident), incidentId.ToString());

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers.AnyAsync(d => d.Id == request.DriverId.Value && d.TenantId == tenantId, cancellationToken);
            if (!driverExists) throw new NotFoundException(nameof(Driver), request.DriverId.Value.ToString());
        }

        if (request.EmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees.AnyAsync(e => e.Id == request.EmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!empExists) throw new NotFoundException(nameof(Employee), request.EmployeeId.Value.ToString());
        }

        var participant = new SafetyIncidentParticipant(
            tenantId,
            incidentId,
            request.ParticipantType,
            request.Role,
            request.DriverId,
            request.EmployeeId,
            request.Name,
            request.InjuryReported,
            request.Notes,
            _currentUserContext.UserId);

        _dbContext.SafetyIncidentParticipants.Add(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetParticipantDtoAsync(participant.Id, cancellationToken);
    }

    public async Task RemoveParticipantAsync(Guid incidentId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var p = await _dbContext.SafetyIncidentParticipants
            .FirstOrDefaultAsync(x => x.Id == participantId && x.SafetyIncidentId == incidentId && x.TenantId == tenantId, cancellationToken);

        if (p == null)
            throw new NotFoundException(nameof(SafetyIncidentParticipant), participantId.ToString());

        _dbContext.SafetyIncidentParticipants.Remove(p);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SafetyIncidentVehicleDto> AddVehicleAsync(Guid incidentId, AddIncidentVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var incident = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && x.TenantId == tenantId, cancellationToken);

        if (incident == null)
            throw new NotFoundException(nameof(SafetyIncident), incidentId.ToString());

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle == null)
            throw new NotFoundException(nameof(Vehicle), request.VehicleId.ToString());

        var incVehicle = new SafetyIncidentVehicle(
            tenantId,
            incidentId,
            request.VehicleId,
            request.DamageReported,
            request.DamageDescription,
            request.IsPrimaryVehicle,
            _currentUserContext.UserId);

        _dbContext.SafetyIncidentVehicles.Add(incVehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SafetyIncidentVehicleDto(
            incVehicle.Id,
            incVehicle.TenantId,
            incVehicle.SafetyIncidentId,
            incVehicle.VehicleId,
            vehicle.RegistrationNumber ?? vehicle.VehicleNumber,
            vehicle.DisplayName,
            incVehicle.DamageReported,
            incVehicle.DamageDescription,
            incVehicle.IsPrimaryVehicle,
            incVehicle.CreatedAtUtc);
    }

    public async Task RemoveVehicleAsync(Guid incidentId, Guid incidentVehicleId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var v = await _dbContext.SafetyIncidentVehicles
            .FirstOrDefaultAsync(x => x.Id == incidentVehicleId && x.SafetyIncidentId == incidentId && x.TenantId == tenantId, cancellationToken);

        if (v == null)
            throw new NotFoundException(nameof(SafetyIncidentVehicle), incidentVehicleId.ToString());

        _dbContext.SafetyIncidentVehicles.Remove(v);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SafetyIncidentAssetDto> AddAssetAsync(Guid incidentId, AddIncidentAssetRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var incident = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && x.TenantId == tenantId, cancellationToken);

        if (incident == null)
            throw new NotFoundException(nameof(SafetyIncident), incidentId.ToString());

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.TenantId == tenantId, cancellationToken);

        if (asset == null)
            throw new NotFoundException(nameof(Asset), request.AssetId.ToString());

        var incAsset = new SafetyIncidentAsset(
            tenantId,
            incidentId,
            request.AssetId,
            request.DamageReported,
            request.DamageDescription,
            _currentUserContext.UserId);

        _dbContext.SafetyIncidentAssets.Add(incAsset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SafetyIncidentAssetDto(
            incAsset.Id,
            incAsset.TenantId,
            incAsset.SafetyIncidentId,
            incAsset.AssetId,
            asset.AssetNumber,
            asset.Name,
            incAsset.DamageReported,
            incAsset.DamageDescription,
            incAsset.CreatedAtUtc);
    }

    public async Task RemoveAssetAsync(Guid incidentId, Guid incidentAssetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var a = await _dbContext.SafetyIncidentAssets
            .FirstOrDefaultAsync(x => x.Id == incidentAssetId && x.SafetyIncidentId == incidentId && x.TenantId == tenantId, cancellationToken);

        if (a == null)
            throw new NotFoundException(nameof(SafetyIncidentAsset), incidentAssetId.ToString());

        _dbContext.SafetyIncidentAssets.Remove(a);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SafetyIncidentEvidenceDto> AddEvidenceAsync(Guid incidentId, AddIncidentEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var incident = await _dbContext.SafetyIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && x.TenantId == tenantId, cancellationToken);

        if (incident == null)
            throw new NotFoundException(nameof(SafetyIncident), incidentId.ToString());

        var evidence = new SafetyIncidentEvidence(
            tenantId,
            incidentId,
            request.EvidenceType,
            request.Title,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.CapturedAtUtc,
            DateTime.UtcNow,
            _currentUserContext.UserId,
            request.Notes);

        _dbContext.SafetyIncidentEvidence.Add(evidence);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentEvidenceAdded,
            nameof(SafetyIncidentEvidence),
            evidence.Id.ToString(),
            $"Added safety evidence '{evidence.Title}' ({evidence.FileName})",
            null,
            evidence));

        return MapEvidenceToDto(evidence);
    }

    public async Task RemoveEvidenceAsync(Guid incidentId, Guid evidenceId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var e = await _dbContext.SafetyIncidentEvidence
            .FirstOrDefaultAsync(x => x.Id == evidenceId && x.SafetyIncidentId == incidentId && x.TenantId == tenantId, cancellationToken);

        if (e == null)
            throw new NotFoundException(nameof(SafetyIncidentEvidence), evidenceId.ToString());

        _dbContext.SafetyIncidentEvidence.Remove(e);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyIncidentEvidenceRemoved,
            nameof(SafetyIncidentEvidence),
            e.Id.ToString(),
            $"Removed safety evidence '{e.Title}'"));
    }

    private async Task<SafetyIncidentDto?> GetSummaryDtoByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyIncidents
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Investigation)
            .Include(x => x.Participants)
            .Include(x => x.Vehicles)
            .Include(x => x.Assets)
            .Include(x => x.Evidence)
            .Include(x => x.CorrectiveActions)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToSummaryDto(entity);
    }

    private async Task<SafetyIncidentParticipantDto> GetParticipantDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var p = await _dbContext.SafetyIncidentParticipants
            .AsNoTracking()
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return MapParticipantToDto(p);
    }

    private static SafetyIncidentDto MapToSummaryDto(SafetyIncident i) =>
        new(
            i.Id,
            i.TenantId,
            i.IncidentNumber,
            i.IncidentType,
            i.Severity,
            i.Status,
            i.OccurredAtUtc,
            i.ReportedAtUtc,
            i.BranchId,
            i.Branch?.Name,
            i.LocationId,
            i.Location?.Name,
            i.Latitude,
            i.Longitude,
            i.Title,
            i.Description,
            i.ImmediateActionTaken,
            i.ReportedByUserId,
            null,
            i.InvestigationRequired,
            i.Investigation?.Status ?? SafetyInvestigationStatus.NotStarted,
            i.Participants.Count,
            i.Vehicles.Count,
            i.Assets.Count,
            i.Evidence.Count,
            i.CorrectiveActions.Count,
            i.CorrectiveActions.Count(c => c.Status == CorrectiveActionStatus.Open || c.Status == CorrectiveActionStatus.InProgress || c.Status == CorrectiveActionStatus.Overdue),
            i.ClosedAtUtc,
            i.ClosedByUserId,
            i.CreatedAtUtc);

    private static SafetyIncidentDetailDto MapToDetailDto(SafetyIncident i) =>
        new(
            i.Id,
            i.TenantId,
            i.IncidentNumber,
            i.IncidentType,
            i.Severity,
            i.Status,
            i.OccurredAtUtc,
            i.ReportedAtUtc,
            i.BranchId,
            i.Branch?.Name,
            i.LocationId,
            i.Location?.Name,
            i.Latitude,
            i.Longitude,
            i.Title,
            i.Description,
            i.ImmediateActionTaken,
            i.ReportedByUserId,
            null,
            i.InvestigationRequired,
            i.ClosedAtUtc,
            i.ClosedByUserId,
            i.CreatedAtUtc,
            i.Investigation != null ? MapInvestigationToDto(i.Investigation) : null,
            i.Participants.Select(MapParticipantToDto).ToList(),
            i.Vehicles.Select(MapVehicleToDto).ToList(),
            i.Assets.Select(MapAssetToDto).ToList(),
            i.Evidence.Select(MapEvidenceToDto).ToList(),
            i.CorrectiveActions.Select(MapCorrectiveActionToDto).ToList(),
            i.Violations.Select(MapViolationToDto).ToList());

    private static SafetyIncidentParticipantDto MapParticipantToDto(SafetyIncidentParticipant p) =>
        new(
            p.Id,
            p.TenantId,
            p.SafetyIncidentId,
            p.ParticipantType,
            p.Role,
            p.DriverId,
            p.Driver?.DisplayName,
            p.EmployeeId,
            p.Employee != null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : null,
            p.Name,
            p.InjuryReported,
            p.Notes,
            p.CreatedAtUtc);

    private static SafetyIncidentVehicleDto MapVehicleToDto(SafetyIncidentVehicle v) =>
        new(
            v.Id,
            v.TenantId,
            v.SafetyIncidentId,
            v.VehicleId,
            v.Vehicle != null ? (v.Vehicle.RegistrationNumber ?? v.Vehicle.VehicleNumber) : "N/A",
            v.Vehicle != null ? v.Vehicle.DisplayName : "N/A",
            v.DamageReported,
            v.DamageDescription,
            v.IsPrimaryVehicle,
            v.CreatedAtUtc);

    private static SafetyIncidentAssetDto MapAssetToDto(SafetyIncidentAsset a) =>
        new(
            a.Id,
            a.TenantId,
            a.SafetyIncidentId,
            a.AssetId,
            a.Asset != null ? a.Asset.AssetNumber : "N/A",
            a.Asset != null ? a.Asset.Name : "N/A",
            a.DamageReported,
            a.DamageDescription,
            a.CreatedAtUtc);

    private static SafetyIncidentEvidenceDto MapEvidenceToDto(SafetyIncidentEvidence e) =>
        new(
            e.Id,
            e.TenantId,
            e.SafetyIncidentId,
            e.EvidenceType,
            e.Title,
            e.FileObjectKey,
            e.FileName,
            e.ContentType,
            e.FileSizeBytes,
            e.CapturedAtUtc,
            e.UploadedAtUtc,
            e.UploadedByUserId,
            e.Notes);

    private static SafetyIncidentInvestigationDto MapInvestigationToDto(SafetyIncidentInvestigation inv) =>
        new(
            inv.Id,
            inv.TenantId,
            inv.SafetyIncidentId,
            inv.InvestigatorEmployeeId,
            inv.InvestigatorEmployee != null ? $"{inv.InvestigatorEmployee.FirstName} {inv.InvestigatorEmployee.LastName}" : null,
            inv.StartedAtUtc,
            inv.CompletedAtUtc,
            inv.Summary,
            inv.RootCause,
            inv.RootCauseDescription,
            inv.ContributingFactors,
            inv.Recommendation,
            inv.Status,
            inv.CreatedAtUtc);

    private static CorrectiveActionDto MapCorrectiveActionToDto(CorrectiveAction c) =>
        new(
            c.Id,
            c.TenantId,
            c.Title,
            c.Description,
            c.Priority,
            c.DueDateUtc,
            c.AssignedEmployeeId,
            c.AssignedEmployee != null ? $"{c.AssignedEmployee.FirstName} {c.AssignedEmployee.LastName}" : null,
            c.SafetyIncidentId,
            null,
            c.SafetyViolationId,
            null,
            c.ComplianceRecordId,
            null,
            c.Status,
            c.DueDateUtc.HasValue && c.DueDateUtc.Value < DateTime.UtcNow && c.Status is CorrectiveActionStatus.Open or CorrectiveActionStatus.InProgress,
            c.CompletedAtUtc,
            c.CompletedByUserId,
            null,
            c.VerificationRequired,
            c.VerifiedAtUtc,
            c.VerifiedByUserId,
            null,
            c.ResolutionNotes,
            c.CreatedAtUtc);

    private static SafetyViolationDto MapViolationToDto(SafetyViolation v) =>
        new(
            v.Id,
            v.TenantId,
            v.ViolationType,
            v.Severity,
            v.Source,
            v.OccurredAtUtc,
            v.Description,
            v.DriverId,
            v.Driver?.DisplayName,
            v.EmployeeId,
            null,
            v.VehicleId,
            null,
            v.SafetyIncidentId,
            null,
            v.Reference,
            v.IsResolved,
            v.ResolvedAtUtc,
            v.ResolvedByUserId,
            null,
            v.ResolutionNotes,
            v.Notes,
            v.CreatedAtUtc);
}

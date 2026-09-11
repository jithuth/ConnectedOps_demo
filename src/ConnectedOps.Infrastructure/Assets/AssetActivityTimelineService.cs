using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetActivityTimelineService : IAssetActivityTimelineService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AssetActivityTimelineService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<AssetActivityTimelineDto> GetTimelineByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.LocationHistories).ThenInclude(lh => lh.Branch)
            .Include(x => x.LocationHistories).ThenInclude(lh => lh.Location)
            .Include(x => x.EmployeeAssignments).ThenInclude(ea => ea.Employee)
            .Include(x => x.VehicleAssignments).ThenInclude(va => va.Vehicle)
            .Include(x => x.UsageSessions).ThenInclude(us => us.Employee)
            .Include(x => x.Transfers)
            .Include(x => x.ConditionRecords)
            .Include(x => x.Inspections)
            .Include(x => x.CalibrationRecords)
            .Include(x => x.Documents)
            .Include(x => x.NotesList)
            .FirstOrDefaultAsync(x => x.Id == assetId && x.TenantId == tenantId, cancellationToken);

        if (asset is null)
            throw new KeyNotFoundException($"Asset '{assetId}' was not found.");

        var events = new List<AssetTimelineEventDto>();

        // 1. Asset creation
        events.Add(new AssetTimelineEventDto(
            Guid.NewGuid(),
            asset.Id,
            "Registration",
            "Asset Created",
            $"Registered asset {asset.AssetNumber} ({asset.Name})",
            asset.CreatedAtUtc,
            asset.CreatedBy?.ToString(),
            null,
            "badge-primary",
            "fas fa-plus-circle"));

        // 2. Location movements
        foreach (var loc in asset.LocationHistories)
        {
            var dest = loc.Branch?.Name ?? loc.Location?.Name ?? "Updated Location";
            events.Add(new AssetTimelineEventDto(
                loc.Id,
                asset.Id,
                "Location",
                "Location Changed",
                $"Moved to {dest}. Reason: {loc.Reason}",
                loc.EffectiveFromUtc,
                loc.ChangedByUserId?.ToString(),
                null,
                "badge-info",
                "fas fa-map-marker-alt"));
        }

        // 3. Employee assignments
        foreach (var ea in asset.EmployeeAssignments)
        {
            events.Add(new AssetTimelineEventDto(
                ea.Id,
                asset.Id,
                "Custody",
                "Employee Assigned",
                $"Assigned to {ea.Employee?.FirstName} {ea.Employee?.LastName} (Condition: {ea.ConditionAtAssignment})",
                ea.AssignedFromUtc,
                ea.AssignedByUserId?.ToString(),
                null,
                "badge-success",
                "fas fa-user-check"));

            if (!ea.IsActive && ea.AssignedToUtc.HasValue)
            {
                events.Add(new AssetTimelineEventDto(
                    Guid.NewGuid(),
                    asset.Id,
                    "Custody",
                    "Employee Returned",
                    $"Returned from {ea.Employee?.FirstName} {ea.Employee?.LastName} (Condition: {ea.ConditionAtReturn})",
                    ea.AssignedToUtc.Value,
                    ea.ReturnedByUserId?.ToString(),
                    null,
                    "badge-secondary",
                    "fas fa-user-minus"));
            }
        }

        // 4. Vehicle assignments
        foreach (var va in asset.VehicleAssignments)
        {
            events.Add(new AssetTimelineEventDto(
                va.Id,
                asset.Id,
                "Vehicle",
                "Vehicle Assigned",
                $"Mounted/assigned to vehicle {va.Vehicle?.RegistrationNumber ?? va.Vehicle?.VehicleNumber}",
                va.AssignedFromUtc,
                va.AssignedByUserId?.ToString(),
                null,
                "badge-info",
                "fas fa-truck"));

            if (!va.IsActive && va.AssignedToUtc.HasValue)
            {
                events.Add(new AssetTimelineEventDto(
                    Guid.NewGuid(),
                    asset.Id,
                    "Vehicle",
                    "Vehicle Removed",
                    $"Removed from vehicle {va.Vehicle?.RegistrationNumber ?? va.Vehicle?.VehicleNumber}",
                    va.AssignedToUtc.Value,
                    va.EndedByUserId?.ToString(),
                    null,
                    "badge-secondary",
                    "fas fa-truck-pickup"));
            }
        }

        // 5. Usage Sessions
        foreach (var us in asset.UsageSessions)
        {
            events.Add(new AssetTimelineEventDto(
                us.Id,
                asset.Id,
                "Session",
                "Checked Out",
                $"Checked out to {us.Employee?.FirstName} {us.Employee?.LastName}. Purpose: {us.Purpose ?? "Operational use"}",
                us.CheckedOutAtUtc,
                us.CheckedOutByUserId?.ToString(),
                null,
                "badge-warning",
                "fas fa-sign-out-alt"));

            if (us.CheckedInAtUtc.HasValue)
            {
                events.Add(new AssetTimelineEventDto(
                    Guid.NewGuid(),
                    asset.Id,
                    "Session",
                    "Checked In",
                    $"Checked in by {us.Employee?.FirstName} {us.Employee?.LastName} (Condition: {us.ConditionAtCheckin})",
                    us.CheckedInAtUtc.Value,
                    us.CheckedInByUserId?.ToString(),
                    null,
                    "badge-success",
                    "fas fa-sign-in-alt"));
            }
        }

        // 6. Transfers
        foreach (var tr in asset.Transfers)
        {
            events.Add(new AssetTimelineEventDto(
                tr.Id,
                asset.Id,
                "Transfer",
                $"Transfer {tr.Status}",
                $"Transfer type: {tr.TransferType}. Reason: {tr.Reason}",
                tr.CompletedAtUtc ?? tr.RequestedAtUtc,
                tr.RequestedByUserId?.ToString(),
                null,
                tr.Status == Domain.Assets.AssetTransferStatus.Completed ? "badge-success" : "badge-secondary",
                "fas fa-exchange-alt"));
        }

        // 7. Condition assessments
        foreach (var cr in asset.ConditionRecords)
        {
            events.Add(new AssetTimelineEventDto(
                cr.Id,
                asset.Id,
                "Condition",
                $"Condition Assessment: {cr.Condition}",
                $"{cr.Description}",
                cr.RecordedAtUtc,
                cr.RecordedByUserId?.ToString(),
                null,
                cr.Condition == Domain.Assets.AssetCondition.NeedsAttention || cr.Condition == Domain.Assets.AssetCondition.Damaged ? "badge-danger" : "badge-info",
                "fas fa-heartbeat"));
        }

        // 8. Inspections
        foreach (var ins in asset.Inspections)
        {
            events.Add(new AssetTimelineEventDto(
                ins.Id,
                asset.Id,
                "Inspection",
                $"{ins.InspectionType} Inspection ({ins.Result})",
                $"Result: {ins.Result}. Notes: {ins.Notes}",
                ins.InspectionDateUtc,
                ins.CompletedByUserId?.ToString(),
                null,
                ins.Result == Domain.Assets.AssetInspectionResult.Failed ? "badge-danger" : "badge-success",
                "fas fa-clipboard-check"));
        }

        // 9. Calibrations
        foreach (var cal in asset.CalibrationRecords)
        {
            events.Add(new AssetTimelineEventDto(
                cal.Id,
                asset.Id,
                "Calibration",
                "Calibration Performed",
                $"Performed by {cal.Provider}. Result: {cal.Result}",
                cal.CalibrationDateUtc,
                cal.CreatedBy?.ToString(),
                null,
                cal.Result == "Passed" ? "badge-success" : "badge-danger",
                "fas fa-tachometer-alt"));
        }

        // 10. Documents
        foreach (var doc in asset.Documents)
        {
            events.Add(new AssetTimelineEventDto(
                doc.Id,
                asset.Id,
                "Document",
                $"Document Attached: {doc.DocumentType}",
                $"Attached file {doc.Title}",
                doc.CreatedAtUtc,
                doc.CreatedBy?.ToString(),
                null,
                "badge-secondary",
                "fas fa-paperclip"));
        }

        // 11. Notes
        foreach (var note in asset.NotesList)
        {
            events.Add(new AssetTimelineEventDto(
                note.Id,
                asset.Id,
                "Note",
                "Operational Note",
                note.NoteText,
                note.CreatedAtUtc,
                note.CreatedByUserId?.ToString(),
                note.CreatedByUserName,
                "badge-dark",
                "fas fa-sticky-note"));
        }

        var sortedEvents = events.OrderByDescending(x => x.TimestampUtc).ToList();

        return new AssetActivityTimelineDto(
            asset.Id,
            asset.AssetNumber,
            asset.Name,
            sortedEvents);
    }
}

using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceEvaluationService : IComplianceEvaluationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IComplianceStatusService _statusService;
    private readonly IComplianceExpiryService _expiryService;
    private readonly IComplianceScoreService _scoreService;

    public ComplianceEvaluationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IComplianceStatusService statusService,
        IComplianceExpiryService expiryService,
        IComplianceScoreService scoreService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _statusService = statusService;
        _expiryService = expiryService;
        _scoreService = scoreService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<ComplianceEvaluationResultDto> EvaluateVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Registrations)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle == null)
            throw new NotFoundException(nameof(Vehicle), vehicleId.ToString());

        var requirements = await GetApplicableRequirementsAsync(
            ComplianceSubjectType.Vehicle,
            vehicleCategoryId: vehicle.VehicleCategoryId,
            branchId: vehicle.BranchId,
            cancellationToken: cancellationToken);

        var directRecords = await _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);

        var exceptions = await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId && x.Status == ComplianceExceptionStatus.Approved)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var requirementResults = new List<ComplianceRequirementItemResultDto>();

        foreach (var req in requirements)
        {
            var hasActiveException = exceptions.Any(e => e.ComplianceRequirementId == req.Id && e.EffectiveFromUtc <= now && now <= e.EffectiveToUtc);
            var direct = directRecords.FirstOrDefault(r => r.ComplianceRequirementId == req.Id);

            DateTime? issueDate = direct?.IssueDateUtc;
            DateTime? expiryDate = direct?.ExpiryDateUtc;
            string? refNum = direct?.ReferenceNumber;
            Guid? authoritativeId = direct?.Id;
            string? sourceType = direct != null ? "DirectComplianceRecord" : null;
            bool isVerified = direct?.VerifiedAtUtc.HasValue ?? false;
            bool recordExists = direct != null;

            // Check authoritative sources if direct record not present or needs fallback
            if (!recordExists)
            {
                if (req.RequirementType == ComplianceRequirementType.Registration)
                {
                    var currentReg = vehicle.Registrations.FirstOrDefault(r => r.IsCurrent);
                    if (currentReg != null)
                    {
                        recordExists = true;
                        issueDate = currentReg.RegistrationDate.HasValue ? currentReg.RegistrationDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        expiryDate = currentReg.ExpiryDate.HasValue ? currentReg.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        refNum = currentReg.RegistrationNumber;
                        authoritativeId = currentReg.Id;
                        sourceType = "VehicleRegistration";
                        isVerified = true;
                    }
                }
                else if (req.RequirementType is ComplianceRequirementType.Insurance or ComplianceRequirementType.Inspection or ComplianceRequirementType.Permit or ComplianceRequirementType.Certification)
                {
                    var matchingDoc = vehicle.Documents.FirstOrDefault(d => d.IsActive && MatchesVehicleDocType(req.RequirementType, d.DocumentType));
                    if (matchingDoc != null)
                    {
                        recordExists = true;
                        issueDate = matchingDoc.IssueDate.HasValue ? matchingDoc.IssueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        expiryDate = matchingDoc.ExpiryDate.HasValue ? matchingDoc.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        refNum = matchingDoc.DocumentNumber ?? matchingDoc.Title;
                        authoritativeId = matchingDoc.Id;
                        sourceType = "VehicleDocument";
                        isVerified = true;
                    }
                }
            }

            var status = _statusService.DetermineStatus(expiryDate, req.DefaultReminderDays, isVerified, hasActiveException, recordExists, now);
            int? daysRemaining = expiryDate.HasValue ? _expiryService.CalculateDaysRemaining(expiryDate, now) : null;

            requirementResults.Add(new ComplianceRequirementItemResultDto(
                req.Id,
                req.Code,
                req.Name,
                req.RequirementType,
                req.ValidityType,
                req.IsMandatory,
                status,
                issueDate,
                expiryDate,
                daysRemaining,
                refNum,
                authoritativeId,
                sourceType,
                hasActiveException,
                null));
        }

        var scoreResult = _scoreService.CalculateScore(requirementResults);
        var overallStatus = _statusService.DetermineOverallStatus(requirementResults.Select(r => r.Status));

        return new ComplianceEvaluationResultDto(
            vehicle.Id,
            ComplianceSubjectType.Vehicle,
            vehicle.RegistrationNumber ?? vehicle.VehicleNumber,
            vehicle.DisplayName,
            vehicle.BranchId,
            vehicle.Branch?.Name,
            overallStatus,
            scoreResult.Score,
            scoreResult.Grade,
            requirementResults.Count,
            requirementResults.Count(r => r.Status == ComplianceStatus.Valid),
            requirementResults.Count(r => r.Status == ComplianceStatus.ExpiringSoon),
            requirementResults.Count(r => r.Status == ComplianceStatus.Expired),
            requirementResults.Count(r => r.Status == ComplianceStatus.Missing),
            requirementResults.Count(r => r.Status == ComplianceStatus.PendingVerification),
            requirementResults.Count(r => r.Status == ComplianceStatus.Rejected || r.Status == ComplianceStatus.Suspended),
            requirementResults.Count(r => r.HasActiveException),
            now,
            requirementResults);
    }

    public async Task<ComplianceEvaluationResultDto> EvaluateDriverAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var driver = await _dbContext.Drivers
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Licenses)
            .Include(x => x.Certifications)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == driverId && x.TenantId == tenantId, cancellationToken);

        if (driver == null)
            throw new NotFoundException(nameof(Driver), driverId.ToString());

        var requirements = await GetApplicableRequirementsAsync(
            ComplianceSubjectType.Driver,
            driverType: driver.DriverType,
            countryCode: driver.NationalityCode,
            branchId: driver.BranchId,
            cancellationToken: cancellationToken);

        var directRecords = await _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Where(x => x.TenantId == tenantId && x.DriverId == driverId)
            .ToListAsync(cancellationToken);

        var exceptions = await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.DriverId == driverId && x.Status == ComplianceExceptionStatus.Approved)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var requirementResults = new List<ComplianceRequirementItemResultDto>();

        foreach (var req in requirements)
        {
            var hasActiveException = exceptions.Any(e => e.ComplianceRequirementId == req.Id && e.EffectiveFromUtc <= now && now <= e.EffectiveToUtc);
            var direct = directRecords.FirstOrDefault(r => r.ComplianceRequirementId == req.Id);

            DateTime? issueDate = direct?.IssueDateUtc;
            DateTime? expiryDate = direct?.ExpiryDateUtc;
            string? refNum = direct?.ReferenceNumber;
            Guid? authoritativeId = direct?.Id;
            string? sourceType = direct != null ? "DirectComplianceRecord" : null;
            bool isVerified = direct?.VerifiedAtUtc.HasValue ?? false;
            bool recordExists = direct != null;

            if (!recordExists)
            {
                if (req.RequirementType == ComplianceRequirementType.License)
                {
                    var license = driver.Licenses.FirstOrDefault(l => l.IsActive && l.IsPrimary) ?? driver.Licenses.FirstOrDefault(l => l.IsActive);
                    if (license != null)
                    {
                        recordExists = true;
                        issueDate = license.IssueDate.HasValue ? license.IssueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        expiryDate = license.ExpiryDate.HasValue ? license.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        refNum = license.LicenseNumber;
                        authoritativeId = license.Id;
                        sourceType = "DriverLicense";
                        isVerified = true;
                    }
                }
                else if (req.RequirementType is ComplianceRequirementType.Certification or ComplianceRequirementType.Training or ComplianceRequirementType.MedicalFitness)
                {
                    var cert = driver.Certifications.FirstOrDefault(c => c.IsActive && MatchesDriverCertType(req.RequirementType, c.CertificationType));
                    if (cert != null)
                    {
                        recordExists = true;
                        issueDate = cert.IssueDate.HasValue ? cert.IssueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        expiryDate = cert.ExpiryDate.HasValue ? cert.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        refNum = cert.CertificateNumber ?? cert.Title;
                        authoritativeId = cert.Id;
                        sourceType = "DriverCertification";
                        isVerified = true;
                    }
                }
            }

            var status = _statusService.DetermineStatus(expiryDate, req.DefaultReminderDays, isVerified, hasActiveException, recordExists, now);
            int? daysRemaining = expiryDate.HasValue ? _expiryService.CalculateDaysRemaining(expiryDate, now) : null;

            requirementResults.Add(new ComplianceRequirementItemResultDto(
                req.Id,
                req.Code,
                req.Name,
                req.RequirementType,
                req.ValidityType,
                req.IsMandatory,
                status,
                issueDate,
                expiryDate,
                daysRemaining,
                refNum,
                authoritativeId,
                sourceType,
                hasActiveException,
                null));
        }

        var scoreResult = _scoreService.CalculateScore(requirementResults);
        var overallStatus = _statusService.DetermineOverallStatus(requirementResults.Select(r => r.Status));

        return new ComplianceEvaluationResultDto(
            driver.Id,
            ComplianceSubjectType.Driver,
            driver.DriverNumber,
            driver.DisplayName,
            driver.BranchId,
            driver.Branch?.Name,
            overallStatus,
            scoreResult.Score,
            scoreResult.Grade,
            requirementResults.Count,
            requirementResults.Count(r => r.Status == ComplianceStatus.Valid),
            requirementResults.Count(r => r.Status == ComplianceStatus.ExpiringSoon),
            requirementResults.Count(r => r.Status == ComplianceStatus.Expired),
            requirementResults.Count(r => r.Status == ComplianceStatus.Missing),
            requirementResults.Count(r => r.Status == ComplianceStatus.PendingVerification),
            requirementResults.Count(r => r.Status == ComplianceStatus.Rejected || r.Status == ComplianceStatus.Suspended),
            requirementResults.Count(r => r.HasActiveException),
            now,
            requirementResults);
    }

    public async Task<ComplianceEvaluationResultDto> EvaluateAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Inspections)
            .Include(x => x.CalibrationRecords)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == assetId && x.TenantId == tenantId, cancellationToken);

        if (asset == null)
            throw new NotFoundException(nameof(Asset), assetId.ToString());

        var requirements = await GetApplicableRequirementsAsync(
            ComplianceSubjectType.Asset,
            assetCategoryId: asset.AssetCategoryId,
            branchId: asset.BranchId,
            cancellationToken: cancellationToken);

        var directRecords = await _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .ToListAsync(cancellationToken);

        var exceptions = await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId && x.Status == ComplianceExceptionStatus.Approved)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var requirementResults = new List<ComplianceRequirementItemResultDto>();

        foreach (var req in requirements)
        {
            var hasActiveException = exceptions.Any(e => e.ComplianceRequirementId == req.Id && e.EffectiveFromUtc <= now && now <= e.EffectiveToUtc);
            var direct = directRecords.FirstOrDefault(r => r.ComplianceRequirementId == req.Id);

            DateTime? issueDate = direct?.IssueDateUtc;
            DateTime? expiryDate = direct?.ExpiryDateUtc;
            string? refNum = direct?.ReferenceNumber;
            Guid? authoritativeId = direct?.Id;
            string? sourceType = direct != null ? "DirectComplianceRecord" : null;
            bool isVerified = direct?.VerifiedAtUtc.HasValue ?? false;
            bool recordExists = direct != null;

            if (!recordExists)
            {
                if (req.RequirementType == ComplianceRequirementType.Inspection)
                {
                    var latestInspection = asset.Inspections
                        .Where(i => i.Status == AssetInspectionStatus.Completed && i.Result != AssetInspectionResult.Failed)
                        .OrderByDescending(i => i.InspectionDateUtc)
                        .FirstOrDefault();

                    if (latestInspection != null)
                    {
                        recordExists = true;
                        issueDate = latestInspection.InspectionDateUtc;
                        expiryDate = latestInspection.NextInspectionDateUtc;
                        refNum = $"INSP-{latestInspection.Id.ToString()[..8].ToUpperInvariant()}";
                        authoritativeId = latestInspection.Id;
                        sourceType = "AssetInspection";
                        isVerified = true;
                    }
                }
                else if (req.RequirementType == ComplianceRequirementType.Calibration)
                {
                    var latestCalibration = asset.CalibrationRecords
                        .OrderByDescending(c => c.CalibrationDateUtc)
                        .FirstOrDefault();

                    if (latestCalibration != null)
                    {
                        recordExists = true;
                        issueDate = latestCalibration.CalibrationDateUtc;
                        expiryDate = latestCalibration.NextCalibrationDateUtc;
                        refNum = latestCalibration.CertificateNumber ?? $"CAL-{latestCalibration.Id.ToString()[..8].ToUpperInvariant()}";
                        authoritativeId = latestCalibration.Id;
                        sourceType = "AssetCalibrationRecord";
                        isVerified = true;
                    }
                }
                else if (req.RequirementType is ComplianceRequirementType.Certification or ComplianceRequirementType.SafetyCheck or ComplianceRequirementType.Insurance)
                {
                    var doc = asset.Documents.FirstOrDefault(d => MatchesAssetDocType(req.RequirementType, d.DocumentType));
                    if (doc != null)
                    {
                        recordExists = true;
                        issueDate = doc.IssueDate.HasValue ? doc.IssueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        expiryDate = doc.ExpiryDate.HasValue ? doc.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
                        refNum = doc.DocumentNumber ?? doc.Title;
                        authoritativeId = doc.Id;
                        sourceType = "AssetDocument";
                        isVerified = true;
                    }
                }
            }

            var status = _statusService.DetermineStatus(expiryDate, req.DefaultReminderDays, isVerified, hasActiveException, recordExists, now);
            int? daysRemaining = expiryDate.HasValue ? _expiryService.CalculateDaysRemaining(expiryDate, now) : null;

            requirementResults.Add(new ComplianceRequirementItemResultDto(
                req.Id,
                req.Code,
                req.Name,
                req.RequirementType,
                req.ValidityType,
                req.IsMandatory,
                status,
                issueDate,
                expiryDate,
                daysRemaining,
                refNum,
                authoritativeId,
                sourceType,
                hasActiveException,
                null));
        }

        var scoreResult = _scoreService.CalculateScore(requirementResults);
        var overallStatus = _statusService.DetermineOverallStatus(requirementResults.Select(r => r.Status));

        return new ComplianceEvaluationResultDto(
            asset.Id,
            ComplianceSubjectType.Asset,
            asset.AssetNumber,
            $"{asset.AssetNumber} - {asset.Name}",
            asset.BranchId,
            asset.Branch?.Name,
            overallStatus,
            scoreResult.Score,
            scoreResult.Grade,
            requirementResults.Count,
            requirementResults.Count(r => r.Status == ComplianceStatus.Valid),
            requirementResults.Count(r => r.Status == ComplianceStatus.ExpiringSoon),
            requirementResults.Count(r => r.Status == ComplianceStatus.Expired),
            requirementResults.Count(r => r.Status == ComplianceStatus.Missing),
            requirementResults.Count(r => r.Status == ComplianceStatus.PendingVerification),
            requirementResults.Count(r => r.Status == ComplianceStatus.Rejected || r.Status == ComplianceStatus.Suspended),
            requirementResults.Count(r => r.HasActiveException),
            now,
            requirementResults);
    }

    public async Task<IReadOnlyList<SubjectComplianceSummaryDto>> EvaluateAllSubjectsAsync(
        ComplianceSubjectType? subjectType = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var summaries = new List<SubjectComplianceSummaryDto>();

        if (!subjectType.HasValue || subjectType.Value == ComplianceSubjectType.Vehicle)
        {
            var vehicleQuery = _dbContext.Vehicles.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive);
            if (branchId.HasValue) vehicleQuery = vehicleQuery.Where(x => x.BranchId == branchId.Value);
            var vehicleIds = await vehicleQuery.Select(x => x.Id).ToListAsync(cancellationToken);

            foreach (var vid in vehicleIds)
            {
                var eval = await EvaluateVehicleAsync(vid, cancellationToken);
                summaries.Add(new SubjectComplianceSummaryDto(
                    eval.SubjectId,
                    eval.SubjectType,
                    eval.SubjectIdentifier,
                    eval.SubjectDisplayName,
                    eval.BranchId,
                    eval.BranchName,
                    eval.OverallStatus,
                    eval.Score,
                    eval.Grade,
                    eval.ValidCount,
                    eval.ExpiringCount,
                    eval.ExpiredCount,
                    eval.MissingCount,
                    eval.EvaluatedAtUtc));
            }
        }

        if (!subjectType.HasValue || subjectType.Value == ComplianceSubjectType.Driver)
        {
            var driverQuery = _dbContext.Drivers.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive);
            if (branchId.HasValue) driverQuery = driverQuery.Where(x => x.BranchId == branchId.Value);
            var driverIds = await driverQuery.Select(x => x.Id).ToListAsync(cancellationToken);

            foreach (var did in driverIds)
            {
                var eval = await EvaluateDriverAsync(did, cancellationToken);
                summaries.Add(new SubjectComplianceSummaryDto(
                    eval.SubjectId,
                    eval.SubjectType,
                    eval.SubjectIdentifier,
                    eval.SubjectDisplayName,
                    eval.BranchId,
                    eval.BranchName,
                    eval.OverallStatus,
                    eval.Score,
                    eval.Grade,
                    eval.ValidCount,
                    eval.ExpiringCount,
                    eval.ExpiredCount,
                    eval.MissingCount,
                    eval.EvaluatedAtUtc));
            }
        }

        if (!subjectType.HasValue || subjectType.Value == ComplianceSubjectType.Asset)
        {
            var assetQuery = _dbContext.Assets.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive);
            if (branchId.HasValue) assetQuery = assetQuery.Where(x => x.BranchId == branchId.Value);
            var assetIds = await assetQuery.Select(x => x.Id).ToListAsync(cancellationToken);

            foreach (var aid in assetIds)
            {
                var eval = await EvaluateAssetAsync(aid, cancellationToken);
                summaries.Add(new SubjectComplianceSummaryDto(
                    eval.SubjectId,
                    eval.SubjectType,
                    eval.SubjectIdentifier,
                    eval.SubjectDisplayName,
                    eval.BranchId,
                    eval.BranchName,
                    eval.OverallStatus,
                    eval.Score,
                    eval.Grade,
                    eval.ValidCount,
                    eval.ExpiringCount,
                    eval.ExpiredCount,
                    eval.MissingCount,
                    eval.EvaluatedAtUtc));
            }
        }

        return summaries;
    }

    private async Task<IReadOnlyList<ComplianceRequirement>> GetApplicableRequirementsAsync(
        ComplianceSubjectType subjectType,
        Guid? vehicleCategoryId = null,
        Guid? assetCategoryId = null,
        DriverType? driverType = null,
        string? countryCode = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var allReqs = await _dbContext.ComplianceRequirements
            .AsNoTracking()
            .Include(x => x.Rules)
            .Where(x => x.TenantId == tenantId && x.AppliesTo == subjectType && x.IsActive)
            .ToListAsync(cancellationToken);

        var applicable = new List<ComplianceRequirement>();

        foreach (var req in allReqs)
        {
            if (req.Rules.Count == 0)
            {
                // No rules means applies universally to all subjects of this type
                applicable.Add(req);
                continue;
            }

            var activeRules = req.Rules.Where(r => r.IsActive).ToList();
            if (activeRules.Count == 0)
            {
                applicable.Add(req);
                continue;
            }

            // Check if any rule matches
            bool matches = activeRules.Any(rule =>
            {
                if (rule.VehicleCategoryId.HasValue && vehicleCategoryId.HasValue && rule.VehicleCategoryId != vehicleCategoryId.Value)
                    return false;

                if (rule.AssetCategoryId.HasValue && assetCategoryId.HasValue && rule.AssetCategoryId != assetCategoryId.Value)
                    return false;

                if (rule.DriverType.HasValue && driverType.HasValue && rule.DriverType != driverType.Value)
                    return false;

                if (!string.IsNullOrWhiteSpace(rule.CountryCode) && !string.IsNullOrWhiteSpace(countryCode) &&
                    !string.Equals(rule.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (rule.BranchId.HasValue && branchId.HasValue && rule.BranchId != branchId.Value)
                    return false;

                return true;
            });

            if (matches)
            {
                applicable.Add(req);
            }
        }

        return applicable;
    }

    private static bool MatchesVehicleDocType(ComplianceRequirementType reqType, VehicleDocumentType docType)
    {
        return reqType switch
        {
            ComplianceRequirementType.Insurance => docType == VehicleDocumentType.Insurance,
            ComplianceRequirementType.Inspection => docType is VehicleDocumentType.Inspection or VehicleDocumentType.FitnessCertificate,
            ComplianceRequirementType.Permit => docType == VehicleDocumentType.RoadPermit,
            ComplianceRequirementType.Certification => docType == VehicleDocumentType.EmissionCertificate,
            _ => false
        };
    }

    private static bool MatchesDriverCertType(ComplianceRequirementType reqType, CertificationType certType)
    {
        return reqType switch
        {
            ComplianceRequirementType.Certification => true,
            ComplianceRequirementType.Training => certType is CertificationType.SafetyTraining or CertificationType.DefensiveDriving or CertificationType.FirstAid or CertificationType.HeavyVehicleTraining,
            ComplianceRequirementType.MedicalFitness => true,
            _ => true
        };
    }

    private static bool MatchesAssetDocType(ComplianceRequirementType reqType, AssetDocumentType docType)
    {
        return reqType switch
        {
            ComplianceRequirementType.Inspection => docType is AssetDocumentType.InspectionCertificate,
            ComplianceRequirementType.Calibration => docType is AssetDocumentType.CalibrationCertificate,
            ComplianceRequirementType.SafetyCheck => docType is AssetDocumentType.SafetyCertificate,
            ComplianceRequirementType.Insurance => docType is AssetDocumentType.Insurance,
            ComplianceRequirementType.Certification => docType is AssetDocumentType.SafetyCertificate or AssetDocumentType.Warranty,
            _ => false
        };
    }
}

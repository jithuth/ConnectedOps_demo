using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceEvaluationService
{
    Task<ComplianceEvaluationResultDto> EvaluateVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<ComplianceEvaluationResultDto> EvaluateDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<ComplianceEvaluationResultDto> EvaluateAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubjectComplianceSummaryDto>> EvaluateAllSubjectsAsync(ComplianceSubjectType? subjectType = null, Guid? branchId = null, CancellationToken cancellationToken = default);
}

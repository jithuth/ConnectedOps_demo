using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceExceptionService
{
    Task<PagedResult<ComplianceExceptionDto>> GetPagedAsync(ComplianceExceptionFilterRequest filter, CancellationToken cancellationToken = default);
    Task<ComplianceExceptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceExceptionDto> CreateAsync(CreateComplianceExceptionRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceExceptionDto> ApproveAsync(Guid id, ApproveComplianceExceptionRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceExceptionDto> RejectAsync(Guid id, RejectComplianceExceptionRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceExceptionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveExceptionAsync(Guid requirementId, Guid subjectId, CancellationToken cancellationToken = default);
}

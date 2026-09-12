using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceRecordService
{
    Task<PagedResult<ComplianceRecordDto>> GetPagedAsync(ComplianceRecordFilterRequest filter, CancellationToken cancellationToken = default);
    Task<ComplianceRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceRecordDto> CreateAsync(CreateComplianceRecordRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceRecordDto> UpdateAsync(Guid id, UpdateComplianceRecordRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceRecordDto> VerifyAsync(Guid id, VerifyComplianceRecordRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceDocumentDto> AddDocumentAsync(Guid recordId, AddComplianceDocumentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceDocumentDto>> GetDocumentsAsync(Guid recordId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid recordId, Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceRecordDto>> GetSubjectRecordsAsync(ComplianceSubjectType subjectType, Guid subjectId, CancellationToken cancellationToken = default);
}

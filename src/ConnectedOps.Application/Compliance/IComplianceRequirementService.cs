using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceRequirementService
{
    Task<PagedResult<ComplianceRequirementDto>> GetPagedAsync(ComplianceRequirementFilterRequest filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceRequirementDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<ComplianceRequirementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceRequirementDto> CreateAsync(CreateComplianceRequirementRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceRequirementDto> UpdateAsync(Guid id, UpdateComplianceRequirementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceRequirementRuleDto> AddRuleAsync(CreateComplianceRequirementRuleRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceRequirementRuleDto> UpdateRuleAsync(Guid requirementId, Guid ruleId, UpdateComplianceRequirementRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid requirementId, Guid ruleId, CancellationToken cancellationToken = default);
}

using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Hos;

public interface IHosService
{
    Task<HosRuleConfigurationDto> GetPolicyAsync(CancellationToken cancellationToken = default);

    Task<HosRuleConfigurationDto> UpdatePolicyAsync(UpdateHosPolicyRequest request, CancellationToken cancellationToken = default);

    Task<HosDriverClocksDto> GetDriverClocksAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<HosLogEntryDto> ChangeDutyStatusAsync(Guid driverId, ChangeDutyStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<HosLogEntryDto>> GetLogsPagedAsync(HosLogFilterRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<HosViolationDto>> GetViolationsPagedAsync(HosViolationFilterRequest request, CancellationToken cancellationToken = default);

    Task<HosViolationDto> AcknowledgeViolationAsync(Guid violationId, CancellationToken cancellationToken = default);

    Task<HosRoadsideReportDto> GenerateRoadsideReportAsync(Guid driverId, DateTime dateUtc, CancellationToken cancellationToken = default);
}

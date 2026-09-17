using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Integrations;

public interface IErpExportService
{
    Task<ErpExportBatchDto> GenerateBatchAsync(GenerateErpBatchRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ErpExportBatchDto>> GetBatchesPagedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<ErpExportBatchDto?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> MarkBatchExportedAsync(Guid id, string? externalRef = null, CancellationToken cancellationToken = default);
}

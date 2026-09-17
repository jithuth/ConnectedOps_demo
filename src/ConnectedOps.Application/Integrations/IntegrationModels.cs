using ConnectedOps.Domain.Integrations;

namespace ConnectedOps.Application.Integrations;

public sealed record WebhookSubscriptionDto(
    Guid Id,
    string Url,
    string Description,
    string SecretKeyMasked,
    List<WebhookEventType> Events,
    bool IsEnabled,
    int FailureCount,
    DateTime? LastTriggeredAtUtc,
    int? LastResponseStatusCode,
    DateTime CreatedAtUtc);

public sealed record CreateWebhookRequest(
    string Url,
    string Description,
    List<WebhookEventType> Events);

public sealed record UpdateWebhookRequest(
    string Url,
    string Description,
    List<WebhookEventType> Events);

public sealed record WebhookDeliveryAttemptDto(
    Guid Id,
    Guid SubscriptionId,
    WebhookEventType EventType,
    string EventTypeName,
    int ResponseStatusCode,
    long DurationMs,
    bool Success,
    string? ErrorMessage,
    DateTime AttemptedAtUtc,
    string PayloadJson);

public sealed record TenantApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    List<ApiKeyScope> Scopes,
    DateTime? ExpiresAtUtc,
    DateTime? LastUsedAtUtc,
    bool IsActive,
    bool IsExpired,
    DateTime CreatedAtUtc);

public sealed record CreateApiKeyRequest(
    string Name,
    List<ApiKeyScope> Scopes,
    DateTime? ExpiresAtUtc = null);

public sealed record ApiKeyCreatedResultDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    string PlaintextApiKey,
    List<ApiKeyScope> Scopes,
    DateTime? ExpiresAtUtc);

public sealed record ErpExportBatchDto(
    Guid Id,
    string BatchNumber,
    ErpTargetSystem TargetSystem,
    string TargetSystemName,
    ErpBatchType BatchType,
    string BatchTypeName,
    ErpBatchStatus Status,
    string StatusName,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int RecordCount,
    decimal TotalAmount,
    string Currency,
    DateTime? ExportedAtUtc,
    string? ExternalReference,
    string? ErrorMessage,
    string PayloadJson);

public sealed record GenerateErpBatchRequest(
    ErpTargetSystem TargetSystem,
    ErpBatchType BatchType,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc);

public sealed record FuelFeedSyncLogDto(
    Guid Id,
    FuelClearinghouseProvider Provider,
    string ProviderName,
    DateTime SyncedAtUtc,
    int TransactionsCount,
    decimal TotalSpend,
    decimal TotalLiters,
    string Status,
    string? ErrorMessage,
    string? RawSummary);

public sealed record TriggerFuelSyncRequest(
    FuelClearinghouseProvider Provider,
    bool SimulateTransactions = true);

public sealed record IntegrationsDashboardDto(
    int TotalWebhooksCount,
    int ActiveWebhooksCount,
    int TotalDeliveriesToday,
    int SuccessfulDeliveriesToday,
    int ActiveApiKeysCount,
    int TotalErpBatchesGenerated,
    int TotalFuelSyncsThisMonth,
    List<WebhookDeliveryAttemptDto> RecentDeliveries,
    List<ErpExportBatchDto> RecentBatches,
    List<FuelFeedSyncLogDto> RecentFuelSyncs);

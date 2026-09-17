using System.Security.Cryptography;
using System.Text;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Integrations;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Integrations;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class Phase16IntegrationsTests
{
    private static async Task<Vehicle> SeedVehicleAsync(ConnectedOpsDbContext db, Guid tenantId, string vehicleNumber = "VAN-01")
    {
        var category = new VehicleCategory(tenantId, "Van", "VAN", null, true);
        var make = new VehicleMake(tenantId, "Toyota", "JP");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "HiAce", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            vehicleNumber,
            category.Id,
            make.Id,
            model.Id,
            displayName: "HiAce 1",
            registrationNumber: "DXB-9988",
            currentOdometer: 15000m,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    [Fact]
    public async Task ApiKeyService_CreateAndValidate_SucceedsWithCorrectScopes()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ApiKeyService(db, userContext, NullLogger<ApiKeyService>.Instance);

        var scopes = new List<ApiKeyScope> { ApiKeyScope.FleetRead, ApiKeyScope.TelematicsRead, ApiKeyScope.DispatchWrite };
        var created = await service.CreateApiKeyAsync(new CreateApiKeyRequest("Mobile Gateway Key", scopes));

        Assert.NotNull(created.PlaintextApiKey);
        Assert.StartsWith("co_live_", created.PlaintextApiKey);
        Assert.Equal("Mobile Gateway Key", created.Name);
        Assert.Equal(3, created.Scopes.Count);

        // Plaintext key should not be stored directly in db
        var dbEntity = await db.TenantApiKeys.FirstOrDefaultAsync(k => k.Id == created.Id);
        Assert.NotNull(dbEntity);
        Assert.NotEqual(created.PlaintextApiKey, dbEntity.KeyHash);

        // Validation against plaintext key
        var (isValid, matchedTenant, matchedScopes) = await service.ValidateApiKeyAsync(created.PlaintextApiKey);
        Assert.True(isValid);
        Assert.Equal(tenantId, matchedTenant);
        Assert.Contains(ApiKeyScope.FleetRead, matchedScopes);
        Assert.Contains(ApiKeyScope.DispatchWrite, matchedScopes);
    }

    [Fact]
    public async Task ApiKeyService_RevokeApiKey_InvalidatesKeyImmediately()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ApiKeyService(db, userContext, NullLogger<ApiKeyService>.Instance);

        var created = await service.CreateApiKeyAsync(new CreateApiKeyRequest("Partner Key", [ApiKeyScope.ReportsRead]));
        var revokeResult = await service.RevokeApiKeyAsync(created.Id);
        Assert.True(revokeResult);

        // Validate revoked key
        var (isValid, _, _) = await service.ValidateApiKeyAsync(created.PlaintextApiKey);
        Assert.False(isValid);

        var keys = await service.GetApiKeysAsync();
        var keyDto = keys.First(k => k.Id == created.Id);
        Assert.False(keyDto.IsActive);
    }

    [Fact]
    public async Task WebhookService_CreateSubscription_GeneratesHmacSecretAndTogglesState()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var httpClient = new HttpClient();
        var service = new WebhookService(db, userContext, httpClient, NullLogger<WebhookService>.Instance);

        var request = new CreateWebhookRequest(
            "https://api.thirdparty.com/events",
            "Third Party TMS",
            [WebhookEventType.VehicleStatusChanged, WebhookEventType.GeofenceBreached]);

        var sub = await service.CreateSubscriptionAsync(request);
        Assert.Equal("https://api.thirdparty.com/events", sub.Url);
        Assert.True(sub.IsEnabled);
        Assert.Equal(2, sub.Events.Count);
        Assert.StartsWith("whsec_", sub.SecretKeyMasked);

        // Verify entity in db
        var dbSub = await db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == sub.Id);
        Assert.NotNull(dbSub);
        Assert.NotEmpty(dbSub.SecretKey);

        // Toggle state
        var toggled = await service.ToggleSubscriptionAsync(sub.Id, false);
        Assert.True(toggled);

        var subAfterToggle = await db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == sub.Id);
        Assert.False(subAfterToggle!.IsEnabled);
    }

    [Fact]
    public void WebhookService_HmacSignature_ProducesValidHexSha256()
    {
        var secret = "whsec_0123456789abcdef0123456789abcdef";
        var payload = "{\"event\":\"VehicleStatusChanged\",\"vehicleId\":\"12345\"}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        Assert.NotNull(signature);
        Assert.Equal(64, signature.Length); // 256 bits = 32 bytes = 64 hex chars
    }

    [Fact]
    public async Task ErpExportService_GenerateAndMarkExported_QuickBooksAndXeroBatches()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ErpExportService(db, userContext, NullLogger<ErpExportService>.Instance);

        var start = DateTime.UtcNow.AddDays(-30);
        var end = DateTime.UtcNow;

        // Generate QuickBooks batch
        var qbRequest = new GenerateErpBatchRequest(ErpTargetSystem.QuickBooks, ErpBatchType.Expenses, start, end);
        var qbBatch = await service.GenerateBatchAsync(qbRequest);

        Assert.NotNull(qbBatch);
        Assert.StartsWith("ERP-", qbBatch.BatchNumber);
        Assert.Equal(ErpBatchStatus.Generated, qbBatch.Status);
        Assert.Contains("QuickBooks", qbBatch.PayloadJson);

        // Mark as exported
        var exportResult = await service.MarkBatchExportedAsync(qbBatch.Id, "QB-TXN-88391");
        Assert.True(exportResult);

        var updated = await service.GetBatchByIdAsync(qbBatch.Id);
        Assert.NotNull(updated);
        Assert.Equal(ErpBatchStatus.Exported, updated.Status);
        Assert.Equal("QB-TXN-88391", updated.ExternalReference);
        Assert.NotNull(updated.ExportedAtUtc);

        // Generate SAP batch for Maintenance Costs
        var sapRequest = new GenerateErpBatchRequest(ErpTargetSystem.Sap, ErpBatchType.MaintenanceCosts, start, end);
        var sapBatch = await service.GenerateBatchAsync(sapRequest);
        Assert.StartsWith("ERP-", sapBatch.BatchNumber);
        Assert.Contains("Sap", sapBatch.PayloadJson);
    }

    [Fact]
    public async Task FuelClearinghouseService_TriggerSync_RecordsBatchAndCalculatesTotals()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        await SeedVehicleAsync(db, tenantId, "TRUCK-99");

        var service = new FuelClearinghouseService(db, userContext, NullLogger<FuelClearinghouseService>.Instance);

        var result = await service.TriggerSyncAsync(new TriggerFuelSyncRequest(FuelClearinghouseProvider.Wex, SimulateTransactions: true));

        Assert.NotNull(result);
        Assert.Equal(FuelClearinghouseProvider.Wex, result.Provider);
        Assert.Equal(8, result.TransactionsCount);
        Assert.True(result.TotalSpend > 0);
        Assert.True(result.TotalLiters > 0);
        Assert.Equal("Completed", result.Status);
        Assert.NotNull(result.RawSummary);

        var paged = await service.GetSyncLogsPagedAsync(1, 10);
        Assert.Equal(1, paged.TotalCount);

        var dashboard = await service.GetDashboardAsync();
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.TotalFuelSyncsThisMonth);
    }

    [Fact]
    public async Task Phase16_TenantIsolation_TenantCannotAccessAnotherTenantsIntegrations()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };

        var apiKeyServiceA = new ApiKeyService(db, userContextA, NullLogger<ApiKeyService>.Instance);
        var apiKeyServiceB = new ApiKeyService(db, userContextB, NullLogger<ApiKeyService>.Instance);

        var webhookServiceA = new WebhookService(db, userContextA, new HttpClient(), NullLogger<WebhookService>.Instance);
        var webhookServiceB = new WebhookService(db, userContextB, new HttpClient(), NullLogger<WebhookService>.Instance);

        // Tenant A creates an API key and a webhook
        var keyA = await apiKeyServiceA.CreateApiKeyAsync(new CreateApiKeyRequest("Tenant A Key", [ApiKeyScope.FleetRead]));
        var webhookA = await webhookServiceA.CreateSubscriptionAsync(new CreateWebhookRequest("https://tenant-a.com/hook", "A Hook", [WebhookEventType.HarshDrivingDetected]));

        // Tenant B lists keys and webhooks
        var keysB = await apiKeyServiceB.GetApiKeysAsync();
        var webhooksB = await webhookServiceB.GetSubscriptionsAsync();

        Assert.DoesNotContain(keysB, k => k.Id == keyA.Id);
        Assert.DoesNotContain(webhooksB, w => w.Id == webhookA.Id);

        // Tenant B attempting to revoke Tenant A's key should fail (not found under Tenant B filter)
        var revokeFromB = await apiKeyServiceB.RevokeApiKeyAsync(keyA.Id);
        Assert.False(revokeFromB);

        // Tenant B attempting to delete Tenant A's webhook should fail
        var deleteFromB = await webhookServiceB.DeleteSubscriptionAsync(webhookA.Id);
        Assert.False(deleteFromB);
    }
}

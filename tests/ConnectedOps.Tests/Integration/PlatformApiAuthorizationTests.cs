using System.Net;
using System.Net.Http.Headers;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConnectedOps.Tests.Integration;

public sealed class PlatformApiAuthorizationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly IJwtTokenService _jwtService;

    public PlatformApiAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public async Task AnonymousAccess_ToPlatformTenants_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/platform/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TenantTokenAccess_ToPlatformTenants_Returns403Forbidden()
    {
        var tenantUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "tenantuser@company.com",
            Email = "tenantuser@company.com",
            PlatformRole = PlatformRole.None
        };
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();

        var (token, _) = _jwtService.CreateAccessToken(tenantUser, tenantId, tenantUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdminTokenAccess_ToPlatformTenants_Returns200Ok()
    {
        var superAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "superadmin@connectedops.com",
            Email = "superadmin@connectedops.com",
            PlatformRole = PlatformRole.SuperAdmin
        };

        var (token, _) = _jwtService.CreatePlatformAccessToken(superAdmin);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TenantSelectionToken_ToPlatformApi_Returns401Unauthorized()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "multitenantuser@corp.com",
            Email = "multitenantuser@corp.com",
            PlatformRole = PlatformRole.None
        };

        var (token, _) = _jwtService.CreateTenantSelectionToken(user);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Tenant-selection tokens must fail API authentication
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

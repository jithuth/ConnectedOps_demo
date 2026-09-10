using System.Net;
using System.Text.Json;
using Xunit;

namespace ConnectedOps.Tests.Integration;

public sealed class HealthChecksApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLiveEndpoint_Returns200AndHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var status = jsonDoc.RootElement.GetProperty("status").GetString();

        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task HealthReadyEndpoint_Returns200AndHealthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var status = jsonDoc.RootElement.GetProperty("status").GetString();

        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task FullHealthEndpoint_Returns200WithDiagnosticDetails()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);

        var status = jsonDoc.RootElement.GetProperty("status").GetString();
        var version = jsonDoc.RootElement.GetProperty("version").GetString();
        var checks = jsonDoc.RootElement.GetProperty("checks");

        Assert.Equal("Healthy", status);
        Assert.Equal("1.23.0", version);
        Assert.True(checks.GetArrayLength() >= 2);
    }
}

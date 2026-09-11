using ConnectedOps.Application.Telematics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

/// <summary>
/// Telemetry ingestion endpoint. Runs outside the normal JWT tenant authentication.
/// Device TenantId is resolved strictly from the registered device record — never from the client payload.
/// </summary>
[ApiController]
[Route("api/telematics/ingest")]
[AllowAnonymous]
public sealed class TelematicsIngestionController : ControllerBase
{
    private readonly ITelemetryIngestionService _ingestionService;
    private readonly IEnumerable<ITelematicsProvider> _providers;

    public TelematicsIngestionController(
        ITelemetryIngestionService ingestionService,
        IEnumerable<ITelematicsProvider> providers)
    {
        _ingestionService = ingestionService;
        _providers = providers;
    }

    /// <summary>
    /// Ingest pre-normalized telemetry messages in JSON format.
    /// The DeviceIdentifier field in each message is used to look up the registered device;
    /// TenantId is resolved from the device record.
    /// </summary>
    [HttpPost("normalized")]
    public async Task<IActionResult> IngestNormalized(
        [FromBody] NormalizedTelemetryMessage[] messages,
        CancellationToken cancellationToken)
    {
        if (messages == null || messages.Length == 0)
        {
            return BadRequest(new { message = "Payload must contain at least one telemetry message." });
        }

        var results = await _ingestionService.IngestBatchAsync(messages, cancellationToken);
        return Ok(new
        {
            total = results.Count,
            succeeded = results.Count(r => r.Success),
            duplicates = results.Count(r => r.IsDuplicate),
            quarantined = results.Count(r => r.IsQuarantined),
            results
        });
    }

    /// <summary>
    /// Ingest raw Teltonika Codec 8/Extended binary data.
    /// DeviceIdentifier can be passed as a query parameter (IMEI).
    /// </summary>
    [HttpPost("teltonika")]
    public async Task<IActionResult> IngestTeltonika(
        [FromQuery] string? imei,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var payload = ms.ToArray();

        if (payload.Length == 0)
        {
            return BadRequest(new { message = "Empty binary payload received." });
        }

        var provider = _providers.FirstOrDefault(p => p.CanHandle("teltonika"));
        if (provider == null)
        {
            return StatusCode(501, new { message = "Teltonika provider is not configured on this server." });
        }

        var messages = await provider.ParseBinaryPayloadAsync(payload, imei, cancellationToken);
        if (messages.Count == 0)
        {
            return BadRequest(new { message = "Could not decode any telemetry records from the binary payload." });
        }

        var results = await _ingestionService.IngestBatchAsync(messages, cancellationToken);
        return Ok(new
        {
            total = results.Count,
            succeeded = results.Count(r => r.Success),
            duplicates = results.Count(r => r.IsDuplicate),
            quarantined = results.Count(r => r.IsQuarantined),
            results
        });
    }
}

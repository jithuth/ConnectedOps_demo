using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/test-errors")]
[AllowAnonymous]
public sealed class TestErrorsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public TestErrorsController(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private void EnsureDevelopment()
    {
        if (!_environment.IsDevelopment())
        {
            throw new KeyNotFoundException(
                "This endpoint is available only in development.");
        }
    }

    [HttpGet("400")]
    public IActionResult Test400()
    {
        EnsureDevelopment();

        throw new ArgumentException(
            "This is a test bad-request exception.");
    }

    [HttpGet("404")]
    public IActionResult Test404()
    {
        EnsureDevelopment();

        throw new KeyNotFoundException(
            "This is a test not-found exception.");
    }

    [HttpGet("409")]
    public IActionResult Test409()
    {
        EnsureDevelopment();

        throw new InvalidOperationException(
            "This is a test conflict exception.");
    }

    [HttpGet("500")]
    public IActionResult Test500()
    {
        EnsureDevelopment();

        throw new Exception(
            "This is a secret internal exception.");
    }
}
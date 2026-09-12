using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Compliance;

[ApiController]
[Route("api/compliance/records")]
[Authorize]
public sealed class ComplianceRecordsController : ControllerBase
{
    private readonly IComplianceRecordService _recordService;

    public ComplianceRecordsController(IComplianceRecordService recordService)
    {
        _recordService = recordService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.ComplianceRecords.View)]
    public async Task<IActionResult> GetAll([FromQuery] ComplianceRecordFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _recordService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRecords.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _recordService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.ComplianceRecords.Create)]
    public async Task<IActionResult> Create([FromBody] CreateComplianceRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await _recordService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRecords.Edit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateComplianceRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await _recordService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify")]
    [RequirePermission(PermissionKeys.ComplianceRecords.Verify)]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyComplianceRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await _recordService.VerifyAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRecords.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _recordService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/documents")]
    [RequirePermission(PermissionKeys.ComplianceRecords.View)]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _recordService.GetDocumentsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/documents")]
    [RequirePermission(PermissionKeys.ComplianceRecords.Create)]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddComplianceDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await _recordService.AddDocumentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRecords.Edit)]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        await _recordService.DeleteDocumentAsync(id, documentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("subjects/{subjectType}/{subjectId:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRecords.View)]
    public async Task<IActionResult> GetSubjectRecords(ComplianceSubjectType subjectType, Guid subjectId, CancellationToken cancellationToken)
    {
        var result = await _recordService.GetSubjectRecordsAsync(subjectType, subjectId, cancellationToken);
        return Ok(result);
    }
}

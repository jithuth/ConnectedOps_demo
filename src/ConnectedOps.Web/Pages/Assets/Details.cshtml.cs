using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets;

[Authorize]
public sealed class DetailsModel : PageModel
{
    private readonly IAssetService _assetService;
    private readonly IAssetCustodyService _custodyService;
    private readonly IAssetTransferService _transferService;
    private readonly IAssetInspectionService _inspectionService;
    private readonly IAssetDocumentService _documentService;
    private readonly IAssetActivityTimelineService _timelineService;
    private readonly IAssetQrCodeService _qrCodeService;

    public DetailsModel(
        IAssetService assetService,
        IAssetCustodyService custodyService,
        IAssetTransferService transferService,
        IAssetInspectionService inspectionService,
        IAssetDocumentService documentService,
        IAssetActivityTimelineService timelineService,
        IAssetQrCodeService qrCodeService)
    {
        _assetService = assetService;
        _custodyService = custodyService;
        _transferService = transferService;
        _inspectionService = inspectionService;
        _documentService = documentService;
        _timelineService = timelineService;
        _qrCodeService = qrCodeService;
    }

    public AssetDetailDto Asset { get; private set; } = null!;
    public IReadOnlyList<AssetEmployeeAssignmentDto> EmployeeAssignments { get; private set; } = [];
    public IReadOnlyList<AssetVehicleAssignmentDto> VehicleAssignments { get; private set; } = [];
    public AssetUsageSessionDto? ActiveUsageSession { get; private set; }
    public IReadOnlyList<AssetTransferDto> Transfers { get; private set; } = [];
    public IReadOnlyList<AssetInspectionDto> Inspections { get; private set; } = [];
    public IReadOnlyList<AssetCalibrationRecordDto> Calibrations { get; private set; } = [];
    public IReadOnlyList<AssetDocumentDto> Documents { get; private set; } = [];
    public AssetActivityTimelineDto Timeline { get; private set; } = null!;
    public string QrCodeSvg { get; private set; } = string.Empty;

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _assetService.GetByIdAsync(id, cancellationToken);
        if (asset is null) return NotFound();

        Asset = asset;
        EmployeeAssignments = await _custodyService.GetEmployeeAssignmentsByAssetAsync(id, cancellationToken);
        VehicleAssignments = await _custodyService.GetVehicleAssignmentsByAssetAsync(id, cancellationToken);
        ActiveUsageSession = await _custodyService.GetActiveSessionByAssetAsync(id, cancellationToken);
        Transfers = await _transferService.GetTransfersByAssetAsync(id, cancellationToken);
        Inspections = await _inspectionService.GetInspectionsByAssetAsync(id, cancellationToken);
        Calibrations = await _inspectionService.GetCalibrationsByAssetAsync(id, cancellationToken);
        Documents = await _documentService.GetDocumentsByAssetAsync(id, cancellationToken);
        Timeline = await _timelineService.GetTimelineByAssetAsync(id, cancellationToken);

        var qrPayload = $"https://app.connectedops.io/assets/scan/{Asset.Id}";
        QrCodeSvg = _qrCodeService.GenerateSvgQrCode(qrPayload, 8);

        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(
        Guid id,
        AssetStatus newStatus,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _assetService.ChangeStatusAsync(id, new ChangeAssetStatusRequest(newStatus, reason), cancellationToken);
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await OnGetAsync(id, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAddNoteAsync(
        Guid id,
        string note,
        CancellationToken cancellationToken)
    {
        try
        {
            await _assetService.AddNoteAsync(id, new AddAssetNoteRequest(note), cancellationToken);
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await OnGetAsync(id, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostCheckOutAsync(
        Guid id,
        Guid employeeId,
        Guid? vehicleId,
        decimal? meterHours,
        AssetCondition condition,
        DateTime? expectedReturnUtc,
        string? purpose,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new StartAssetUsageSessionRequest(id, employeeId, vehicleId, meterHours, condition, expectedReturnUtc, purpose, notes);
            await _custodyService.CheckOutAsync(req, cancellationToken);
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await OnGetAsync(id, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostCheckInAsync(
        Guid id,
        Guid sessionId,
        decimal? meterHours,
        AssetCondition condition,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new EndAssetUsageSessionRequest(meterHours, condition, notes);
            await _custodyService.CheckInAsync(sessionId, req, cancellationToken);
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await OnGetAsync(id, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostTransferAsync(
        Guid id,
        AssetTransferType transferType,
        Guid? targetBranchId,
        Guid? targetLocationId,
        Guid? targetEmployeeId,
        Guid? targetVehicleId,
        string? reason,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new CreateAssetTransferRequest(id, transferType, targetBranchId, targetLocationId, targetEmployeeId, targetVehicleId, reason, notes);
            await _transferService.CreateTransferAsync(req, cancellationToken);
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await OnGetAsync(id, cancellationToken);
        }
    }
}

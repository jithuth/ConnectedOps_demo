using ConnectedOps.Application.Assets;
using QRCoder;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetQrCodeService : IAssetQrCodeService
{
    public string GenerateSvgQrCode(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var svgQrCode = new SvgQRCode(qrCodeData);
        return svgQrCode.GetGraphic(pixelsPerModule);
    }

    public byte[] GeneratePngQrCode(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}

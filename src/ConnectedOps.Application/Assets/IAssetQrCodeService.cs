namespace ConnectedOps.Application.Assets;

public interface IAssetQrCodeService
{
    string GenerateSvgQrCode(string payload, int pixelsPerModule = 10);
    byte[] GeneratePngQrCode(string payload, int pixelsPerModule = 10);
}

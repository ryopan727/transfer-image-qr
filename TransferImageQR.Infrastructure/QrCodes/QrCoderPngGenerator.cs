using QRCoder;
using TransferImageQR.Application.QrCodes;

namespace TransferImageQR.Infrastructure.QrCodes;

public sealed class QrCoderPngGenerator : IQrCodeGenerator
{
    private const int PixelsPerModule = 12;

    public byte[] CreatePng(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(PixelsPerModule, drawQuietZones: true);
    }
}

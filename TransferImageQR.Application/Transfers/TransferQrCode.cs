namespace TransferImageQR.Application.Transfers;

public sealed record TransferQrCode(Uri Url, byte[] PngBytes);

namespace TransferImageQR.Application.QrCodes;

public interface IQrCodeGenerator
{
    byte[] CreatePng(string content);
}

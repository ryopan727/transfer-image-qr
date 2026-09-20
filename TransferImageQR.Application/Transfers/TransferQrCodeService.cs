using System.Net;
using TransferImageQR.Application.QrCodes;

namespace TransferImageQR.Application.Transfers;

public sealed class TransferQrCodeService(
    ITransferUrlProvider transferUrlProvider,
    IQrCodeGenerator qrCodeGenerator) : ITransferQrCodeService
{
    public TransferQrCode? Create(string sessionToken, IPAddress? address)
    {
        var url = transferUrlProvider.Create(sessionToken, address);
        if (url is null)
        {
            return null;
        }

        return new TransferQrCode(url, qrCodeGenerator.CreatePng(url.AbsoluteUri));
    }
}

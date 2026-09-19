namespace TransferImageQR.Application.Transfers;

public interface ITransferQrCodeService
{
    TransferQrCode? Create(string sessionToken);
}

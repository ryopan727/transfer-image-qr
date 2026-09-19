namespace TransferImageQR.Application.Sessions;

public interface ITransferSessionAccessProvider
{
    TransferSessionAccess GetAccess(string token);
}

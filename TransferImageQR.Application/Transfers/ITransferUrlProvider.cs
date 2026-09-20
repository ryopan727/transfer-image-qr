using System.Net;

namespace TransferImageQR.Application.Transfers;

public interface ITransferUrlProvider
{
    Uri? Create(string sessionToken, IPAddress? address);
}

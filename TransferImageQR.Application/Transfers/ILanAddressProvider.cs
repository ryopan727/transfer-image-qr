using System.Net;

namespace TransferImageQR.Application.Transfers;

public interface ILanAddressProvider
{
    IPAddress? GetPreferredIPv4Address();
}

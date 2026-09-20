using System.Net;
using System.Net.Sockets;

namespace TransferImageQR.Application.Transfers;

public sealed class TransferUrlProvider(IHttpServerEndpoint serverEndpoint) : ITransferUrlProvider
{
    public Uri? Create(string sessionToken, IPAddress? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionToken);

        if (!serverEndpoint.IsRunning ||
            serverEndpoint.Port is < 1 or > 65535 ||
            address?.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        return new UriBuilder(
            Uri.UriSchemeHttp,
            address.ToString(),
            serverEndpoint.Port,
            $"transfer/{Uri.EscapeDataString(sessionToken)}").Uri;
    }
}

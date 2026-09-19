using System.Net.Sockets;

namespace TransferImageQR.Application.Transfers;

public sealed class TransferUrlProvider(
    IHttpServerEndpoint serverEndpoint,
    ILanAddressProvider lanAddressProvider) : ITransferUrlProvider
{
    public Uri? Create(string sessionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionToken);

        var address = lanAddressProvider.GetPreferredIPv4Address();
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

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TransferImageQR.Application.Transfers;

namespace TransferImageQR.Infrastructure.Networking;

public sealed class SystemLanAddressProvider : ILanAddressProvider
{
    public IReadOnlyList<LanAddressOption> GetIPv4Addresses()
    {
        var candidates = new List<LanAddressCandidate>();
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            try
            {
                var properties = networkInterface.GetIPProperties();
                var interfaceIndex = properties.GetIPv4Properties()?.Index ?? int.MaxValue;
                candidates.AddRange(properties.UnicastAddresses
                    .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(address => new LanAddressCandidate(
                        address.Address,
                        networkInterface.Name,
                        networkInterface.NetworkInterfaceType,
                        networkInterface.OperationalStatus,
                        interfaceIndex)));
            }
            catch (NetworkInformationException)
            {
                // Ignore an interface that became unavailable while it was being inspected.
            }
        }

        return SelectUsable(candidates);
    }

    public static IReadOnlyList<LanAddressOption> SelectUsable(
        IEnumerable<LanAddressCandidate> candidates) =>
        candidates
            .Where(candidate =>
                candidate.OperationalStatus == OperationalStatus.Up &&
                IsPrivateIPv4(candidate.Address))
            .OrderBy(candidate => IsPhysicalLan(candidate.InterfaceType) ? 0 : 1)
            .ThenBy(candidate => candidate.InterfaceIndex)
            .ThenBy(candidate => candidate.Address.ToString(), StringComparer.Ordinal)
            .DistinctBy(candidate => candidate.Address)
            .Select(candidate => new LanAddressOption(
                candidate.Address,
                string.IsNullOrWhiteSpace(candidate.InterfaceName)
                    ? "Network Interface"
                    : candidate.InterfaceName))
            .ToArray();

    private static bool IsPrivateIPv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
               bytes[0] == 192 && bytes[1] == 168;
    }

    private static bool IsPhysicalLan(NetworkInterfaceType interfaceType) =>
        interfaceType is NetworkInterfaceType.Ethernet or
            NetworkInterfaceType.GigabitEthernet or
            NetworkInterfaceType.FastEthernetFx or
            NetworkInterfaceType.FastEthernetT or
            NetworkInterfaceType.Wireless80211;
}

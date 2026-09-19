using System.Net;
using System.Net.NetworkInformation;
using TransferImageQR.Infrastructure.Networking;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed class SystemLanAddressProviderTests
{
    [Fact]
    public void SelectUsable_ReturnsPrivateIPv4CandidatesInDeterministicOrder()
    {
        LanAddressCandidate[] candidates =
        [
            new(IPAddress.Parse("172.20.0.1"), "VPN", NetworkInterfaceType.Tunnel, OperationalStatus.Up, 4),
            new(IPAddress.Loopback, "Loopback", NetworkInterfaceType.Loopback, OperationalStatus.Up, 1),
            new(IPAddress.Parse("192.168.1.50"), "Wi-Fi", NetworkInterfaceType.Wireless80211, OperationalStatus.Up, 12),
            new(IPAddress.Parse("192.168.1.20"), "Ethernet", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 7),
            new(IPAddress.Parse("10.0.0.2"), "Down", NetworkInterfaceType.Ethernet, OperationalStatus.Down, 2),
        ];

        var selected = SystemLanAddressProvider.SelectUsable(candidates);

        Assert.Equal(
            ["192.168.1.20", "192.168.1.50", "172.20.0.1"],
            selected.Select(option => option.Address.ToString()));
        Assert.Equal(["Ethernet", "Wi-Fi", "VPN"], selected.Select(option => option.InterfaceName));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.1.2")]
    [InlineData("8.8.8.8")]
    [InlineData("2001:db8::1")]
    public void SelectUsable_ExcludesUnusableAddresses(string address)
    {
        LanAddressCandidate[] candidates =
        [new(IPAddress.Parse(address), "Adapter", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 1)];

        Assert.Empty(SystemLanAddressProvider.SelectUsable(candidates));
    }

    [Fact]
    public void SelectUsable_DeduplicatesSameAddress()
    {
        LanAddressCandidate[] candidates =
        [
            new(IPAddress.Parse("192.168.1.20"), "Ethernet", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 7),
            new(IPAddress.Parse("192.168.1.20"), "Duplicate", NetworkInterfaceType.Tunnel, OperationalStatus.Up, 12),
        ];

        var option = Assert.Single(SystemLanAddressProvider.SelectUsable(candidates));

        Assert.Equal("Ethernet", option.InterfaceName);
    }
}

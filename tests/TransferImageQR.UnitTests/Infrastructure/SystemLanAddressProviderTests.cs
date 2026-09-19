using System.Net;
using System.Net.NetworkInformation;
using TransferImageQR.Infrastructure.Networking;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed class SystemLanAddressProviderTests
{
    [Fact]
    public void SelectPreferred_PrefersPhysicalPrivateIPv4Deterministically()
    {
        LanAddressCandidate[] candidates =
        [
            new(IPAddress.Parse("172.20.0.1"), NetworkInterfaceType.Ethernet3Megabit, OperationalStatus.Up, 4),
            new(IPAddress.Loopback, NetworkInterfaceType.Loopback, OperationalStatus.Up, 1),
            new(IPAddress.Parse("192.168.1.50"), NetworkInterfaceType.Wireless80211, OperationalStatus.Up, 12),
            new(IPAddress.Parse("192.168.1.20"), NetworkInterfaceType.Ethernet, OperationalStatus.Up, 7),
            new(IPAddress.Parse("10.0.0.2"), NetworkInterfaceType.Ethernet, OperationalStatus.Down, 2),
        ];

        var selected = SystemLanAddressProvider.SelectPreferred(candidates);

        Assert.Equal(IPAddress.Parse("192.168.1.20"), selected);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.1.2")]
    [InlineData("8.8.8.8")]
    [InlineData("2001:db8::1")]
    public void SelectPreferred_WithOnlyUnusableAddress_ReturnsNull(string address)
    {
        LanAddressCandidate[] candidates =
        [new(IPAddress.Parse(address), NetworkInterfaceType.Ethernet, OperationalStatus.Up, 1)];

        Assert.Null(SystemLanAddressProvider.SelectPreferred(candidates));
    }
}

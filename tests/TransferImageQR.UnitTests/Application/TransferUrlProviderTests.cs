using System.Net;
using TransferImageQR.Application.Transfers;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class TransferUrlProviderTests
{
    [Fact]
    public void Create_WithRunningServerAndLanAddress_ReturnsTokenizedHttpUrl()
    {
        var sut = new TransferUrlProvider(new StubServerEndpoint(true, 51846));

        var url = sut.Create("abc_123-token", IPAddress.Parse("192.168.1.20"));

        Assert.Equal(
            "http://192.168.1.20:51846/transfer/abc_123-token",
            url?.AbsoluteUri);
    }

    [Theory]
    [InlineData(false, 51846, "192.168.1.20")]
    [InlineData(true, 0, "192.168.1.20")]
    [InlineData(true, 51846, null)]
    public void Create_WithoutUsableEndpoint_ReturnsNull(
        bool isRunning,
        int port,
        string? address)
    {
        var sut = new TransferUrlProvider(new StubServerEndpoint(isRunning, port));

        var url = sut.Create("token", address is null ? null : IPAddress.Parse(address));

        Assert.Null(url);
    }

    private sealed record StubServerEndpoint(bool IsRunning, int Port) : IHttpServerEndpoint;
}

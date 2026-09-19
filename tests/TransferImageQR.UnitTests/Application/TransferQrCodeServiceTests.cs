using TransferImageQR.Application.QrCodes;
using TransferImageQR.Application.Transfers;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class TransferQrCodeServiceTests
{
    [Fact]
    public void Create_UsesCurrentSessionTokenUrlAsQrContent()
    {
        var urlProvider = new StubUrlProvider(
            new Uri("http://192.168.1.20:51846/transfer/current-token"));
        var qrGenerator = new StubQrCodeGenerator();
        var sut = new TransferQrCodeService(urlProvider, qrGenerator);

        var result = sut.Create("current-token");

        Assert.Equal("current-token", urlProvider.SessionToken);
        Assert.Equal(result?.Url.AbsoluteUri, qrGenerator.Content);
        Assert.Equal([1, 2, 3], result?.PngBytes);
    }

    [Fact]
    public void Create_WithoutLanUrl_ReturnsNullWithoutGeneratingQr()
    {
        var qrGenerator = new StubQrCodeGenerator();
        var sut = new TransferQrCodeService(new StubUrlProvider(null), qrGenerator);

        var result = sut.Create("current-token");

        Assert.Null(result);
        Assert.Null(qrGenerator.Content);
    }

    private sealed class StubUrlProvider(Uri? url) : ITransferUrlProvider
    {
        public string? SessionToken { get; private set; }

        public Uri? Create(string sessionToken)
        {
            SessionToken = sessionToken;
            return url;
        }
    }

    private sealed class StubQrCodeGenerator : IQrCodeGenerator
    {
        public string? Content { get; private set; }

        public byte[] CreatePng(string content)
        {
            Content = content;
            return [1, 2, 3];
        }
    }
}

using System.Buffers.Binary;
using TransferImageQR.Infrastructure.QrCodes;
using Xunit;

namespace TransferImageQR.IntegrationTests.QrCodes;

public sealed class QrCoderPngGeneratorTests
{
    [Fact]
    public void CreatePng_WithTransferUrl_ReturnsSquarePngLargeEnoughForCameraScanning()
    {
        var sut = new QrCoderPngGenerator();

        var png = sut.CreatePng("http://192.168.1.20:51846/transfer/abc_123-token");

        Assert.True(png.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        Assert.Equal(width, height);
        Assert.True(width >= 300);
    }
}

using TransferImageQR.Infrastructure.Files;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed class PhysicalFileMetadataProviderTests
{
    [Fact]
    public async Task Get_WithExistingFile_ReturnsExactByteLength()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(filePath, new byte[1025], TestContext.Current.CancellationToken);
            var sut = new PhysicalFileMetadataProvider();

            var result = sut.Get(filePath);

            Assert.True(result.Success);
            Assert.Equal(1025, result.Length);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Get_WithMissingFile_ReturnsFailure()
    {
        var sut = new PhysicalFileMetadataProvider();

        var result = sut.Get(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.jpg"));

        Assert.False(result.Success);
    }
}

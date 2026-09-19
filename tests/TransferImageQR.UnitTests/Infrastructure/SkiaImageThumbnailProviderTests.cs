using SkiaSharp;
using TransferImageQR.Infrastructure.Images;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed class SkiaImageThumbnailProviderTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, ".jpg")]
    [InlineData(SKEncodedImageFormat.Png, ".png")]
    [InlineData(SKEncodedImageFormat.Webp, ".webp")]
    public async Task CreateAsync_WithSupportedImage_ReturnsBoundedPngThumbnail(
        SKEncodedImageFormat sourceFormat,
        string extension)
    {
        var directory = Directory.CreateTempSubdirectory("transfer-image-qr-");
        var filePath = Path.Combine(directory.FullName, $"source{extension}");

        try
        {
            WriteImage(filePath, sourceFormat, width: 400, height: 200);
            var sut = new SkiaImageThumbnailProvider();

            var result = await sut.CreateAsync(
                filePath,
                maximumWidth: 160,
                maximumHeight: 120,
                TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.NotNull(result.PngBytes);
            using var thumbnail = SKBitmap.Decode(result.PngBytes);
            Assert.NotNull(thumbnail);
            Assert.Equal(160, thumbnail.Width);
            Assert.Equal(80, thumbnail.Height);

            File.Delete(filePath);
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_WithUnreadableImage_ReturnsFailure()
    {
        var directory = Directory.CreateTempSubdirectory("transfer-image-qr-");
        var filePath = Path.Combine(directory.FullName, "broken.jpg");

        try
        {
            await File.WriteAllTextAsync(filePath, "not an image", TestContext.Current.CancellationToken);
            var sut = new SkiaImageThumbnailProvider();

            var result = await sut.CreateAsync(
                filePath,
                maximumWidth: 160,
                maximumHeight: 120,
                TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Null(result.PngBytes);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void WriteImage(string filePath, SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality: 90);
        using var stream = File.Create(filePath);
        data.SaveTo(stream);
    }
}

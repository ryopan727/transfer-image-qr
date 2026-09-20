using SkiaSharp;
using TransferImageQR.Infrastructure.Images;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed class SkiaBackgroundImageLoaderTests
{
    [Fact]
    public void LoadAsPng_PreservesSourceAlphaAndDoesNotLockFile()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "TransferImageQR-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "transparent.png");

        try
        {
            using (var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul))
            {
                bitmap.Erase(new SKColor(10, 20, 30, 96));
                using var image = SKImage.FromBitmap(bitmap);
                using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(path, encoded.ToArray());
            }

            var bytes = new SkiaBackgroundImageLoader().LoadAsPng(path);
            using var decoded = SKBitmap.Decode(bytes);

            Assert.NotNull(decoded);
            Assert.InRange(decoded.GetPixel(0, 0).Alpha, (byte)95, (byte)97);
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadAsPng_WithInvalidImage_ThrowsInvalidDataException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "not an image");

        try
        {
            Assert.Throws<InvalidDataException>(() =>
                new SkiaBackgroundImageLoader().LoadAsPng(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadAsPng_WithUnsupportedExtension_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            new SkiaBackgroundImageLoader().LoadAsPng("background.bmp"));
    }
}

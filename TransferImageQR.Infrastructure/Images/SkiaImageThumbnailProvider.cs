using SkiaSharp;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Infrastructure.Images;

public sealed class SkiaImageThumbnailProvider : IImageThumbnailProvider
{
    public Task<ImageThumbnailResult> CreateAsync(
        string filePath,
        int maximumWidth,
        int maximumHeight,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumHeight);

        return Task.Run(
            () => CreateThumbnail(filePath, maximumWidth, maximumHeight),
            cancellationToken);
    }

    private static ImageThumbnailResult CreateThumbnail(
        string filePath,
        int maximumWidth,
        int maximumHeight)
    {
        try
        {
            using var codec = SKCodec.Create(filePath);
            if (codec is null || !TryMapFormat(codec.EncodedFormat, out var detectedFormat))
            {
                return ImageThumbnailResult.Failed;
            }

            using var source = SKBitmap.Decode(codec);
            if (source is null || source.Width <= 0 || source.Height <= 0)
            {
                return ImageThumbnailResult.Failed;
            }

            var scale = Math.Min(
                (double)maximumWidth / source.Width,
                (double)maximumHeight / source.Height);
            scale = Math.Min(scale, 1d);

            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            using var resized = source.Resize(imageInfo, new SKSamplingOptions(SKCubicResampler.Mitchell));
            if (resized is null)
            {
                return ImageThumbnailResult.Failed;
            }

            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
            return encoded is null
                ? ImageThumbnailResult.Failed
                : ImageThumbnailResult.Succeeded(encoded.ToArray(), detectedFormat);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            InvalidOperationException)
        {
            return ImageThumbnailResult.Failed;
        }
    }

    private static bool TryMapFormat(SKEncodedImageFormat format, out ImageFileFormat imageFileFormat)
    {
        switch (format)
        {
            case SKEncodedImageFormat.Jpeg:
                imageFileFormat = ImageFileFormat.Jpeg;
                return true;
            case SKEncodedImageFormat.Png:
                imageFileFormat = ImageFileFormat.Png;
                return true;
            case SKEncodedImageFormat.Webp:
                imageFileFormat = ImageFileFormat.WebP;
                return true;
            default:
                imageFileFormat = default;
                return false;
        }
    }
}

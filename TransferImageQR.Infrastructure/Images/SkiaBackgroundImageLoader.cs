using SkiaSharp;
using TransferImageQR.Application.Backgrounds;

namespace TransferImageQR.Infrastructure.Images;

public sealed class SkiaBackgroundImageLoader : IBackgroundImageLoader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    public byte[] LoadAsPng(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!SupportedExtensions.Contains(Path.GetExtension(filePath)))
        {
            throw new InvalidDataException("Only JPEG, PNG, and WebP background images are supported.");
        }

        var sourceBytes = File.ReadAllBytes(filePath);
        using (var codecStream = new SKMemoryStream(sourceBytes))
        using (var codec = SKCodec.Create(codecStream)
            ?? throw new InvalidDataException("The selected file is not a readable image."))
        {
            if (codec.EncodedFormat is not (
                SKEncodedImageFormat.Jpeg or
                SKEncodedImageFormat.Png or
                SKEncodedImageFormat.Webp))
            {
                throw new InvalidDataException("Only JPEG, PNG, and WebP background images are supported.");
            }
        }

        using var bitmap = SKBitmap.Decode(sourceBytes)
            ?? throw new InvalidDataException("The selected file is not a readable image.");
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidDataException("The selected image could not be decoded.");
        return encoded.ToArray();
    }
}

using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Application.Drafts;

public interface IImageThumbnailProvider
{
    Task<ImageThumbnailResult> CreateAsync(
        string filePath,
        int maximumWidth,
        int maximumHeight,
        CancellationToken cancellationToken);
}

public sealed record ImageThumbnailResult
{
    private ImageThumbnailResult(bool success, byte[]? pngBytes, ImageFileFormat? detectedFormat)
    {
        Success = success;
        PngBytes = pngBytes;
        DetectedFormat = detectedFormat;
    }

    public static ImageThumbnailResult Failed { get; } = new(false, null, null);

    public bool Success { get; }

    public byte[]? PngBytes { get; }

    public ImageFileFormat? DetectedFormat { get; }

    public static ImageThumbnailResult Succeeded(byte[] pngBytes, ImageFileFormat detectedFormat)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        return new ImageThumbnailResult(true, pngBytes, detectedFormat);
    }
}

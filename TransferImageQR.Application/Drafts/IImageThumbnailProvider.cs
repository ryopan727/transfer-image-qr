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
    private ImageThumbnailResult(bool success, byte[]? pngBytes)
    {
        Success = success;
        PngBytes = pngBytes;
    }

    public static ImageThumbnailResult Failed { get; } = new(false, null);

    public bool Success { get; }

    public byte[]? PngBytes { get; }

    public static ImageThumbnailResult Succeeded(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        return new ImageThumbnailResult(true, pngBytes);
    }
}

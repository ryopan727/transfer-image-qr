namespace TransferImageQR.Domain.Drafts;

public enum ImageFileFormat
{
    Jpeg,
    Png,
    WebP,
}

public static class ImageFileFormatDetector
{
    public static bool TryDetect(string filePath, out ImageFileFormat format)
    {
        var extension = Path.GetExtension(filePath);

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            format = ImageFileFormat.Jpeg;
            return true;
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            format = ImageFileFormat.Png;
            return true;
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            format = ImageFileFormat.WebP;
            return true;
        }

        format = default;
        return false;
    }
}

namespace TransferImageQR.Application.Backgrounds;

public sealed record BackgroundSettings(
    string? ImagePath,
    int OpacityPercent,
    int ZoomPercent,
    int OffsetX,
    int OffsetY)
{
    public const int DefaultOpacityPercent = 35;
    public const int DefaultZoomPercent = 100;

    public static BackgroundSettings Default { get; } = new(
        null,
        DefaultOpacityPercent,
        DefaultZoomPercent,
        0,
        0);

    public BackgroundSettings Normalize() => new(
        string.IsNullOrWhiteSpace(ImagePath) ? null : Path.GetFullPath(ImagePath),
        Math.Clamp(OpacityPercent, 0, 100),
        Math.Clamp(ZoomPercent, 25, 300),
        Math.Clamp(OffsetX, -1000, 1000),
        Math.Clamp(OffsetY, -1000, 1000));
}

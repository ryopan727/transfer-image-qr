namespace TransferImageQR.Presentation;

internal static class BackgroundImageLayout
{
    public static Rectangle Calculate(
        Size canvasSize,
        Size imageSize,
        int zoomPercent,
        int offsetX,
        int offsetY)
    {
        if (canvasSize.Width <= 0 || canvasSize.Height <= 0 ||
            imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var containScale = Math.Min(
            (double)canvasSize.Width / imageSize.Width,
            (double)canvasSize.Height / imageSize.Height);
        var scale = containScale * Math.Clamp(zoomPercent, 25, 300) / 100d;
        var width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        var height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        return new Rectangle(
            ((canvasSize.Width - width) / 2) + Math.Clamp(offsetX, -1000, 1000),
            ((canvasSize.Height - height) / 2) + Math.Clamp(offsetY, -1000, 1000),
            width,
            height);
    }
}

namespace TransferImageQR.Presentation;

public sealed record BackgroundViewModel(
    byte[]? ImagePng,
    int OpacityPercent,
    int ZoomPercent,
    int OffsetX,
    int OffsetY);

namespace TransferImageQR.Application.Backgrounds;

public enum BackgroundCustomizationError
{
    None,
    ImageUnreadable,
    SettingsCouldNotBeSaved,
}

public sealed record BackgroundCustomizationResult(
    BackgroundSettings Settings,
    byte[]? ImagePng,
    BackgroundCustomizationError Error = BackgroundCustomizationError.None);

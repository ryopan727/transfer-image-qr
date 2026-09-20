namespace TransferImageQR.Application.Backgrounds;

public sealed class BackgroundCustomizationUseCase(
    IBackgroundSettingsStore settingsStore,
    IBackgroundImageLoader imageLoader) : IBackgroundCustomizationUseCase
{
    private BackgroundSettings _settings = BackgroundSettings.Default;
    private byte[]? _imagePng;

    public BackgroundCustomizationResult Load()
    {
        _settings = settingsStore.Load().Normalize();
        if (_settings.ImagePath is null)
        {
            _imagePng = null;
            return Current();
        }

        try
        {
            _imagePng = imageLoader.LoadAsPng(_settings.ImagePath);
            return Current();
        }
        catch (Exception exception) when (IsImageReadFailure(exception))
        {
            _settings = BackgroundSettings.Default;
            _imagePng = null;
            TrySave(_settings);
            return Current(BackgroundCustomizationError.ImageUnreadable);
        }
    }

    public BackgroundCustomizationResult SelectImage(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            var imagePng = imageLoader.LoadAsPng(filePath);
            var next = (_settings with { ImagePath = filePath }).Normalize();
            _settings = next;
            _imagePng = imagePng;
            return Current(TrySave(next));
        }
        catch (Exception exception) when (IsImageReadFailure(exception))
        {
            return Current(BackgroundCustomizationError.ImageUnreadable);
        }
    }

    public BackgroundCustomizationResult UpdateAppearance(
        int opacityPercent,
        int zoomPercent,
        int offsetX,
        int offsetY)
    {
        _settings = (_settings with
        {
            OpacityPercent = opacityPercent,
            ZoomPercent = zoomPercent,
            OffsetX = offsetX,
            OffsetY = offsetY,
        }).Normalize();

        return Current(TrySave(_settings));
    }

    public BackgroundCustomizationResult Clear()
    {
        _settings = BackgroundSettings.Default;
        _imagePng = null;
        return Current(TrySave(_settings));
    }

    private BackgroundCustomizationResult Current(
        BackgroundCustomizationError error = BackgroundCustomizationError.None) =>
        new(_settings, _imagePng, error);

    private BackgroundCustomizationError TrySave(BackgroundSettings settings)
    {
        try
        {
            settingsStore.Save(settings);
            return BackgroundCustomizationError.None;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return BackgroundCustomizationError.SettingsCouldNotBeSaved;
        }
    }

    private static bool IsImageReadFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException;
}

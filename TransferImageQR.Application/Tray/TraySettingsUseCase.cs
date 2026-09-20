namespace TransferImageQR.Application.Tray;

public sealed class TraySettingsUseCase(ITraySettingsStore store) : ITraySettingsUseCase
{
    private TraySettings _settings = TraySettings.Default;

    public TraySettingsResult Load()
    {
        _settings = store.Load();
        return new TraySettingsResult(_settings);
    }

    public TraySettingsResult SetMinimizeToTray(bool enabled)
    {
        _settings = new TraySettings(enabled);
        try
        {
            store.Save(_settings);
            return new TraySettingsResult(_settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new TraySettingsResult(_settings, TraySettingsError.SettingsCouldNotBeSaved);
        }
    }
}

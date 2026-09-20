namespace TransferImageQR.Application.Tray;

public enum TraySettingsError
{
    None,
    SettingsCouldNotBeSaved,
}

public sealed record TraySettingsResult(
    TraySettings Settings,
    TraySettingsError Error = TraySettingsError.None);

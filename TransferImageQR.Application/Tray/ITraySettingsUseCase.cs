namespace TransferImageQR.Application.Tray;

public interface ITraySettingsUseCase
{
    TraySettingsResult Load();

    TraySettingsResult SetMinimizeToTray(bool enabled);
}

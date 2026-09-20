namespace TransferImageQR.Application.Tray;

public interface ITraySettingsStore
{
    TraySettings Load();

    void Save(TraySettings settings);
}

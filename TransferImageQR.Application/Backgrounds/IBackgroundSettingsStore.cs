namespace TransferImageQR.Application.Backgrounds;

public interface IBackgroundSettingsStore
{
    BackgroundSettings Load();

    void Save(BackgroundSettings settings);
}

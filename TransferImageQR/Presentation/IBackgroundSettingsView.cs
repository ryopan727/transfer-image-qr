namespace TransferImageQR.Presentation;

public interface IBackgroundSettingsView
{
    void DisplaySettings(BackgroundViewModel background);

    void DisplayBackgroundError(string? message);
}

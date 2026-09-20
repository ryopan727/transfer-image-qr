namespace TransferImageQR.Presentation;

public interface IBackgroundSettingsPresenter
{
    void AttachBackgroundSettingsView(IBackgroundSettingsView view);

    void DetachBackgroundSettingsView(IBackgroundSettingsView view);

    void SelectBackgroundImage(string filePath);

    void UpdateBackgroundAppearance(
        int opacityPercent,
        int zoomPercent,
        int offsetX,
        int offsetY);

    void ClearBackground();
}

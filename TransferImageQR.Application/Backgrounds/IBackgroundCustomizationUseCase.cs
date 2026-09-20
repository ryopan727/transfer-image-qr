namespace TransferImageQR.Application.Backgrounds;

public interface IBackgroundCustomizationUseCase
{
    BackgroundCustomizationResult Load();

    BackgroundCustomizationResult SelectImage(string filePath);

    BackgroundCustomizationResult UpdateAppearance(
        int opacityPercent,
        int zoomPercent,
        int offsetX,
        int offsetY);

    BackgroundCustomizationResult Clear();
}

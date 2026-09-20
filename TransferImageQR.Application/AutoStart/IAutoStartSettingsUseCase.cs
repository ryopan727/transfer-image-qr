namespace TransferImageQR.Application.AutoStart;

public interface IAutoStartSettingsUseCase
{
    AutoStartSettingsResult Load();

    AutoStartSettingsResult SetEnabled(bool enabled);
}

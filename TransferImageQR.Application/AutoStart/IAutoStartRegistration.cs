namespace TransferImageQR.Application.AutoStart;

public interface IAutoStartRegistration
{
    bool IsEnabled(string executablePath);

    void Enable(string executablePath);

    void Disable();
}

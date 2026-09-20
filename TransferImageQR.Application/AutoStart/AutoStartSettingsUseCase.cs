using System.Security;

namespace TransferImageQR.Application.AutoStart;

public sealed class AutoStartSettingsUseCase(
    IAutoStartRegistration registration,
    string? executablePath) : IAutoStartSettingsUseCase
{
    private bool _enabled;

    public AutoStartSettingsResult Load()
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return Unavailable();
        }

        try
        {
            _enabled = registration.IsEnabled(executablePath);
            return new AutoStartSettingsResult(_enabled);
        }
        catch (Exception exception) when (IsRegistrationError(exception))
        {
            return Unavailable();
        }
    }

    public AutoStartSettingsResult SetEnabled(bool enabled)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return Unavailable();
        }

        try
        {
            if (enabled)
            {
                registration.Enable(executablePath);
            }
            else
            {
                registration.Disable();
            }

            _enabled = enabled;
            return new AutoStartSettingsResult(_enabled);
        }
        catch (Exception exception) when (IsRegistrationError(exception))
        {
            return Unavailable();
        }
    }

    private AutoStartSettingsResult Unavailable() =>
        new(_enabled, AutoStartSettingsError.RegistrationUnavailable);

    private static bool IsRegistrationError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException;
}

namespace TransferImageQR.Application.AutoStart;

public enum AutoStartSettingsError
{
    None,
    RegistrationUnavailable,
}

public sealed record AutoStartSettingsResult(
    bool Enabled,
    AutoStartSettingsError Error = AutoStartSettingsError.None);

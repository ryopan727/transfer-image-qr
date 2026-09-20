namespace TransferImageQR.Application.Tray;

public sealed record TraySettings(bool MinimizeToTray)
{
    public static TraySettings Default { get; } = new(false);
}

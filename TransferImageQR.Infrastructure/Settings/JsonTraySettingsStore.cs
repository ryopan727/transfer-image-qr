using System.Text.Json;
using TransferImageQR.Application.Tray;

namespace TransferImageQR.Infrastructure.Settings;

public sealed class JsonTraySettingsStore(string settingsFilePath) : ITraySettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public TraySettings Load()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return TraySettings.Default;
            }

            var document = JsonSerializer.Deserialize<SettingsDocument>(
                File.ReadAllText(settingsFilePath));
            return document?.Version == 1
                ? new TraySettings(document.MinimizeToTray ?? false)
                : TraySettings.Default;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return TraySettings.Default;
        }
    }

    public void Save(TraySettings settings)
    {
        var directory = Path.GetDirectoryName(settingsFilePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("A settings directory is required.");
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = settingsFilePath + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(
                    new SettingsDocument(1, settings.MinimizeToTray),
                    SerializerOptions));
            File.Move(temporaryPath, settingsFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record SettingsDocument(int Version, bool? MinimizeToTray);
}

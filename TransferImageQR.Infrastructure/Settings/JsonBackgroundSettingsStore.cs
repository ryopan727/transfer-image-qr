using System.Text.Json;
using TransferImageQR.Application.Backgrounds;

namespace TransferImageQR.Infrastructure.Settings;

public sealed class JsonBackgroundSettingsStore(string settingsFilePath) : IBackgroundSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public BackgroundSettings Load()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return BackgroundSettings.Default;
            }

            var json = File.ReadAllText(settingsFilePath);
            return (JsonSerializer.Deserialize<SettingsDocument>(json)?.ToSettings()
                ?? BackgroundSettings.Default).Normalize();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return BackgroundSettings.Default;
        }
    }

    public void Save(BackgroundSettings settings)
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
            var normalized = settings.Normalize();
            var document = new SettingsDocument(
                1,
                normalized.ImagePath,
                normalized.OpacityPercent,
                normalized.ZoomPercent,
                normalized.OffsetX,
                normalized.OffsetY);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions));
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

    private sealed record SettingsDocument(
        int Version,
        string? ImagePath,
        int? OpacityPercent,
        int? ZoomPercent,
        int? OffsetX,
        int? OffsetY)
    {
        public BackgroundSettings ToSettings() => Version == 1
            ? new BackgroundSettings(
                ImagePath,
                OpacityPercent ?? BackgroundSettings.DefaultOpacityPercent,
                ZoomPercent ?? BackgroundSettings.DefaultZoomPercent,
                OffsetX ?? 0,
                OffsetY ?? 0)
            : BackgroundSettings.Default;
    }
}

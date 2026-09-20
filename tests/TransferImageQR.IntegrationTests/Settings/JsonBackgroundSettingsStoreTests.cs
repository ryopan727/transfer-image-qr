using TransferImageQR.Application.Backgrounds;
using TransferImageQR.Infrastructure.Settings;
using Xunit;

namespace TransferImageQR.IntegrationTests.Settings;

public sealed class JsonBackgroundSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "TransferImageQR-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoad_RoundTripsNormalizedSettings()
    {
        var path = Path.Combine(_directory, "background-settings.json");
        var sut = new JsonBackgroundSettingsStore(path);

        sut.Save(new BackgroundSettings("image.png", 72, 160, -25, 40));
        var loaded = sut.Load();

        Assert.Equal(Path.GetFullPath("image.png"), loaded.ImagePath);
        Assert.Equal(72, loaded.OpacityPercent);
        Assert.Equal(160, loaded.ZoomPercent);
        Assert.Equal(-25, loaded.OffsetX);
        Assert.Equal(40, loaded.OffsetY);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_WithMalformedJson_ReturnsDefault()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "background-settings.json");
        File.WriteAllText(path, "{not-json");

        var loaded = new JsonBackgroundSettingsStore(path).Load();

        Assert.Equal(BackgroundSettings.Default, loaded);
    }

    [Fact]
    public void Load_WithMissingAppearanceValues_UsesDocumentedDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "background-settings.json");
        File.WriteAllText(path, """
            {
              "Version": 1,
              "ImagePath": null
            }
            """);

        var loaded = new JsonBackgroundSettingsStore(path).Load();

        Assert.Equal(BackgroundSettings.Default, loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

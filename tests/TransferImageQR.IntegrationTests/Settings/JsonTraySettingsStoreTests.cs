using TransferImageQR.Application.Tray;
using TransferImageQR.Infrastructure.Settings;
using Xunit;

namespace TransferImageQR.IntegrationTests.Settings;

public sealed class JsonTraySettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "TransferImageQR-tests",
        Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveAndLoad_RoundTripsSetting(bool enabled)
    {
        var path = Path.Combine(_directory, "tray-settings.json");
        var sut = new JsonTraySettingsStore(path);

        sut.Save(new TraySettings(enabled));
        var loaded = sut.Load();

        Assert.Equal(enabled, loaded.MinimizeToTray);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_WithMissingMalformedOrUnknownDocument_ReturnsDefault()
    {
        var path = Path.Combine(_directory, "tray-settings.json");
        var sut = new JsonTraySettingsStore(path);
        Assert.Equal(TraySettings.Default, sut.Load());

        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{not-json");
        Assert.Equal(TraySettings.Default, sut.Load());

        File.WriteAllText(path, "{\"Version\":2,\"MinimizeToTray\":true}");
        Assert.Equal(TraySettings.Default, sut.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

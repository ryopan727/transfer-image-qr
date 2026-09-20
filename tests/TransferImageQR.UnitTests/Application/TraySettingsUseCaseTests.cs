using TransferImageQR.Application.Tray;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class TraySettingsUseCaseTests
{
    [Fact]
    public void Load_ReturnsPersistedSetting()
    {
        var sut = new TraySettingsUseCase(new FakeStore(new TraySettings(true)));

        var result = sut.Load();

        Assert.True(result.Settings.MinimizeToTray);
        Assert.Equal(TraySettingsError.None, result.Error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetMinimizeToTray_PersistsSelectedValue(bool enabled)
    {
        var store = new FakeStore(TraySettings.Default);
        var sut = new TraySettingsUseCase(store);

        var result = sut.SetMinimizeToTray(enabled);

        Assert.Equal(enabled, result.Settings.MinimizeToTray);
        Assert.Equal(enabled, store.Saved?.MinimizeToTray);
    }

    [Fact]
    public void SetMinimizeToTray_WhenSaveFails_KeepsCurrentValueAndReturnsError()
    {
        var sut = new TraySettingsUseCase(
            new FakeStore(TraySettings.Default, new IOException()));

        var result = sut.SetMinimizeToTray(true);

        Assert.True(result.Settings.MinimizeToTray);
        Assert.Equal(TraySettingsError.SettingsCouldNotBeSaved, result.Error);
    }

    private sealed class FakeStore(TraySettings loaded, Exception? saveException = null)
        : ITraySettingsStore
    {
        public TraySettings? Saved { get; private set; }

        public TraySettings Load() => loaded;

        public void Save(TraySettings settings)
        {
            if (saveException is not null)
            {
                throw saveException;
            }

            Saved = settings;
        }
    }
}

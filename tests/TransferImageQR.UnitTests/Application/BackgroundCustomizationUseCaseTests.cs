using TransferImageQR.Application.Backgrounds;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class BackgroundCustomizationUseCaseTests
{
    [Fact]
    public void Load_WithSavedImage_ReturnsDecodedImageAndNormalizedAppearance()
    {
        var store = new FakeStore(new BackgroundSettings("image.png", 150, 10, -2000, 2000));
        var loader = new FakeLoader([1, 2, 3]);
        var sut = new BackgroundCustomizationUseCase(store, loader);

        var result = sut.Load();

        Assert.Equal(Path.GetFullPath("image.png"), loader.LoadedPath);
        Assert.Equal([1, 2, 3], result.ImagePng);
        Assert.Equal(100, result.Settings.OpacityPercent);
        Assert.Equal(25, result.Settings.ZoomPercent);
        Assert.Equal(-1000, result.Settings.OffsetX);
        Assert.Equal(1000, result.Settings.OffsetY);
    }

    [Fact]
    public void Load_WhenSavedImageCannotBeRead_FallsBackAndPersistsDefault()
    {
        var store = new FakeStore(new BackgroundSettings("missing.png", 50, 125, 4, 5));
        var sut = new BackgroundCustomizationUseCase(store, new FakeLoader(exception: new IOException()));

        var result = sut.Load();

        Assert.Equal(BackgroundCustomizationError.ImageUnreadable, result.Error);
        Assert.Null(result.ImagePng);
        Assert.Equal(BackgroundSettings.Default, result.Settings);
        Assert.Equal(BackgroundSettings.Default, store.Saved);
    }

    [Fact]
    public void SelectUpdateAndClear_PersistCurrentSettings()
    {
        var store = new FakeStore(BackgroundSettings.Default);
        var sut = new BackgroundCustomizationUseCase(store, new FakeLoader([9, 8]));

        var selected = sut.SelectImage("oshi.png");
        var updated = sut.UpdateAppearance(72, 160, -25, 40);
        var cleared = sut.Clear();

        Assert.Equal([9, 8], selected.ImagePng);
        Assert.Equal(72, updated.Settings.OpacityPercent);
        Assert.Equal(160, updated.Settings.ZoomPercent);
        Assert.Equal(-25, updated.Settings.OffsetX);
        Assert.Equal(40, updated.Settings.OffsetY);
        Assert.Null(cleared.ImagePng);
        Assert.Equal(BackgroundSettings.Default, store.Saved);
    }

    [Fact]
    public void SelectImage_WhenUnreadable_PreservesCurrentBackground()
    {
        var store = new FakeStore(BackgroundSettings.Default);
        var loader = new FakeLoader([4]);
        var sut = new BackgroundCustomizationUseCase(store, loader);
        sut.SelectImage("valid.png");
        loader.Exception = new InvalidDataException();

        var result = sut.SelectImage("broken.png");

        Assert.Equal(BackgroundCustomizationError.ImageUnreadable, result.Error);
        Assert.Equal([4], result.ImagePng);
        Assert.EndsWith("valid.png", result.Settings.ImagePath, StringComparison.Ordinal);
    }

    private sealed class FakeStore(BackgroundSettings loaded) : IBackgroundSettingsStore
    {
        public BackgroundSettings? Saved { get; private set; }

        public BackgroundSettings Load() => loaded;

        public void Save(BackgroundSettings settings) => Saved = settings;
    }

    private sealed class FakeLoader(byte[]? png = null, Exception? exception = null) : IBackgroundImageLoader
    {
        public Exception? Exception { get; set; } = exception;
        public string? LoadedPath { get; private set; }

        public byte[] LoadAsPng(string filePath)
        {
            LoadedPath = filePath;
            if (Exception is not null)
            {
                throw Exception;
            }

            return png ?? [];
        }
    }
}

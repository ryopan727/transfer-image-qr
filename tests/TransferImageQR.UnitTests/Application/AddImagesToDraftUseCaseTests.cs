using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class AddImagesToDraftUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithSupportedImages_AddsEveryReadableImage()
    {
        var thumbnailProvider = new FakeImageThumbnailProvider();
        thumbnailProvider.Succeed(@"C:\images\first.jpg", [1]);
        thumbnailProvider.Succeed(@"C:\images\second.PNG", [2]);
        thumbnailProvider.Succeed(@"C:\images\third.webp", [3]);
        var sut = new AddImagesToDraftUseCase(new TransferDraft(), thumbnailProvider);

        var result = await sut.ExecuteAsync(
            [@"C:\images\first.jpg", @"C:\images\second.PNG", @"C:\images\third.webp"],
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(
            ["first.jpg", "second.PNG", "third.webp"],
            result.AddedImages.Select(image => image.FileName));
        Assert.Equal([1], result.AddedImages[0].ThumbnailPng);
        Assert.Equal([2], result.AddedImages[1].ThumbnailPng);
        Assert.Equal([3], result.AddedImages[2].ThumbnailPng);
    }

    [Fact]
    public async Task ExecuteAsync_AcrossMultipleCalls_AppendsToTheSameDraft()
    {
        var thumbnailProvider = new FakeImageThumbnailProvider();
        thumbnailProvider.Succeed(@"C:\images\first.jpg", [1]);
        thumbnailProvider.Succeed(@"C:\images\second.png", [2]);
        var sut = new AddImagesToDraftUseCase(new TransferDraft(), thumbnailProvider);

        var firstResult = await sut.ExecuteAsync(
            [@"C:\images\first.jpg"],
            TestContext.Current.CancellationToken);
        var secondResult = await sut.ExecuteAsync(
            [@"C:\images\second.png"],
            TestContext.Current.CancellationToken);

        Assert.Equal(1, firstResult.TotalCount);
        Assert.Equal(2, secondResult.TotalCount);
        Assert.Single(secondResult.AddedImages);
        Assert.Equal("second.png", secondResult.AddedImages[0].FileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedInput_AddsValidImagesWithoutRejectingTheBatch()
    {
        var thumbnailProvider = new FakeImageThumbnailProvider();
        thumbnailProvider.Succeed(@"C:\images\valid.png", [1, 2, 3]);
        thumbnailProvider.Fail(@"C:\images\broken.jpeg");
        var sut = new AddImagesToDraftUseCase(new TransferDraft(), thumbnailProvider);

        var result = await sut.ExecuteAsync(
            [@"C:\images\valid.png", @"C:\images\notes.txt", @"C:\images\broken.jpeg"],
            TestContext.Current.CancellationToken);

        var image = Assert.Single(result.AddedImages);
        Assert.Equal("valid.png", image.FileName);
        Assert.Equal(1, result.TotalCount);
        Assert.DoesNotContain(@"C:\images\notes.txt", thumbnailProvider.RequestedPaths);
    }

    private sealed class FakeImageThumbnailProvider : IImageThumbnailProvider
    {
        private readonly Dictionary<string, ImageThumbnailResult> _results = new(StringComparer.OrdinalIgnoreCase);

        public List<string> RequestedPaths { get; } = [];

        public void Succeed(string path, byte[] pngBytes) =>
            _results[path] = ImageThumbnailResult.Succeeded(pngBytes);

        public void Fail(string path) =>
            _results[path] = ImageThumbnailResult.Failed;

        public Task<ImageThumbnailResult> CreateAsync(
            string filePath,
            int maximumWidth,
            int maximumHeight,
            CancellationToken cancellationToken)
        {
            RequestedPaths.Add(filePath);
            return Task.FromResult(_results[filePath]);
        }
    }
}

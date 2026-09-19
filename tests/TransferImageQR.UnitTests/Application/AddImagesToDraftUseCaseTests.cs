using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class AddImagesToDraftUseCaseTests
{
    private const long TenMegabytes = 10L * 1024 * 1024;

    [Fact]
    public async Task ExecuteAsync_WithSupportedImages_AddsEveryReadableImage()
    {
        var dependencies = new FakeImageDependencies();
        dependencies.Succeed(@"C:\images\first.jpg", ImageFileFormat.Jpeg, [1]);
        dependencies.Succeed(@"C:\images\second.PNG", ImageFileFormat.Png, [2]);
        dependencies.Succeed(@"C:\images\third.webp", ImageFileFormat.WebP, [3]);
        var sut = CreateUseCase(new TransferDraft(), dependencies);

        var result = await sut.ExecuteAsync(
            [@"C:\images\first.jpg", @"C:\images\second.PNG", @"C:\images\third.webp"],
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Empty(result.RejectedImages);
        Assert.Equal(
            ["first.jpg", "second.PNG", "third.webp"],
            result.AddedImages.Select(image => image.FileName));
    }

    [Fact]
    public async Task ExecuteAsync_AtTenMegabyteBoundary_AcceptsExactSizeAndRejectsOneByteOver()
    {
        var dependencies = new FakeImageDependencies();
        dependencies.Succeed(@"C:\images\exact.png", ImageFileFormat.Png, [1], TenMegabytes);
        dependencies.Succeed(@"C:\images\over.png", ImageFileFormat.Png, [2], TenMegabytes + 1);
        var sut = CreateUseCase(new TransferDraft(), dependencies);

        var result = await sut.ExecuteAsync(
            [@"C:\images\exact.png", @"C:\images\over.png"],
            TestContext.Current.CancellationToken);

        Assert.Equal("exact.png", Assert.Single(result.AddedImages).FileName);
        var rejection = Assert.Single(result.RejectedImages);
        Assert.Equal("over.png", rejection.FileName);
        Assert.Equal(DraftImageRejectionReason.FileTooLarge, rejection.Reason);
        Assert.DoesNotContain(@"C:\images\over.png", dependencies.ThumbnailRequests);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNineteenImagesExist_AddsOneAndRejectsTheTwentyFirst()
    {
        var draft = new TransferDraft();
        for (var index = 0; index < 19; index++)
        {
            draft.Add($@"C:\images\existing-{index}.jpg");
        }

        var dependencies = new FakeImageDependencies();
        dependencies.Succeed(@"C:\images\twentieth.jpg", ImageFileFormat.Jpeg, [1]);
        dependencies.Succeed(@"C:\images\twenty-first.jpg", ImageFileFormat.Jpeg, [2]);
        var sut = CreateUseCase(draft, dependencies);

        var result = await sut.ExecuteAsync(
            [@"C:\images\twentieth.jpg", @"C:\images\twenty-first.jpg"],
            TestContext.Current.CancellationToken);

        Assert.Equal(20, result.TotalCount);
        Assert.Equal("twentieth.jpg", Assert.Single(result.AddedImages).FileName);
        var rejection = Assert.Single(result.RejectedImages);
        Assert.Equal("twenty-first.jpg", rejection.FileName);
        Assert.Equal(DraftImageRejectionReason.DraftLimitReached, rejection.Reason);
        Assert.DoesNotContain(@"C:\images\twenty-first.jpg", dependencies.MetadataRequests);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedInvalidInput_AddsOnlyValidImagesAndReturnsEachReason()
    {
        var dependencies = new FakeImageDependencies();
        dependencies.Succeed(@"C:\images\valid.png", ImageFileFormat.Png, [1]);
        dependencies.FailThumbnail(@"C:\images\broken.jpeg");
        dependencies.Succeed(@"C:\images\disguised.jpg", ImageFileFormat.Png, [2]);
        dependencies.FailMetadata(@"C:\images\missing.webp");
        var sut = CreateUseCase(new TransferDraft(), dependencies);

        var result = await sut.ExecuteAsync(
            [
                @"C:\images\valid.png",
                @"C:\images\notes.txt",
                @"C:\images\broken.jpeg",
                @"C:\images\disguised.jpg",
                @"C:\images\missing.webp",
            ],
            TestContext.Current.CancellationToken);

        Assert.Equal("valid.png", Assert.Single(result.AddedImages).FileName);
        Assert.Collection(
            result.RejectedImages,
            rejection => Assert.Equal(DraftImageRejectionReason.UnsupportedFormat, rejection.Reason),
            rejection => Assert.Equal(DraftImageRejectionReason.UnreadableImage, rejection.Reason),
            rejection => Assert.Equal(DraftImageRejectionReason.FileFormatMismatch, rejection.Reason),
            rejection => Assert.Equal(DraftImageRejectionReason.UnreadableImage, rejection.Reason));
    }

    private static AddImagesToDraftUseCase CreateUseCase(
        TransferDraft draft,
        FakeImageDependencies dependencies) =>
        new(draft, dependencies, dependencies);

    private sealed class FakeImageDependencies : IImageThumbnailProvider, IFileMetadataProvider
    {
        private readonly Dictionary<string, FileMetadataResult> _metadata = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImageThumbnailResult> _thumbnails = new(StringComparer.OrdinalIgnoreCase);

        public List<string> MetadataRequests { get; } = [];
        public List<string> ThumbnailRequests { get; } = [];

        public void Succeed(string path, ImageFileFormat format, byte[] pngBytes, long length = 1024)
        {
            _metadata[path] = FileMetadataResult.Succeeded(length);
            _thumbnails[path] = ImageThumbnailResult.Succeeded(pngBytes, format);
        }

        public void FailThumbnail(string path)
        {
            _metadata[path] = FileMetadataResult.Succeeded(1024);
            _thumbnails[path] = ImageThumbnailResult.Failed;
        }

        public void FailMetadata(string path) => _metadata[path] = FileMetadataResult.Failed;

        public FileMetadataResult Get(string filePath)
        {
            MetadataRequests.Add(filePath);
            return _metadata[filePath];
        }

        public Task<ImageThumbnailResult> CreateAsync(string filePath, int maximumWidth, int maximumHeight, CancellationToken cancellationToken)
        {
            ThumbnailRequests.Add(filePath);
            return Task.FromResult(_thumbnails[filePath]);
        }
    }
}

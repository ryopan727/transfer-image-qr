using TransferImageQR.Application.Drafts;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class MainPresenterTests
{
    [Fact]
    public async Task AddDroppedFilesAsync_AppendsItemsAndUpdatesDraftCount()
    {
        var useCase = new StubAddImagesToDraftUseCase(
            new AddImagesToDraftResult(
                [
                    new DraftImageListItem(Guid.NewGuid(), @"C:\images\one.jpg", "one.jpg", [1]),
                    new DraftImageListItem(Guid.NewGuid(), @"C:\images\two.png", "two.png", [2]),
                ],
                2,
                []));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, useCase);

        await sut.AddDroppedFilesAsync(
            [@"C:\images\one.jpg", @"C:\images\two.png"],
            TestContext.Current.CancellationToken);

        Assert.Equal(["one.jpg", "two.png"], view.AppendedImages.Select(image => image.FileName));
        Assert.Equal(2, view.DraftCount);
        Assert.Equal([false, true], view.DropEnabledChanges);
    }

    [Fact]
    public async Task AddDroppedFilesAsync_WhenCalledAgain_PreservesPreviouslyDisplayedItems()
    {
        var useCase = new StubAddImagesToDraftUseCase(
            new AddImagesToDraftResult(
                [new DraftImageListItem(Guid.NewGuid(), @"C:\images\one.jpg", "one.jpg", [1])],
                1,
                []),
            new AddImagesToDraftResult(
                [
                    new DraftImageListItem(Guid.NewGuid(), @"C:\images\two.png", "two.png", [2]),
                    new DraftImageListItem(Guid.NewGuid(), @"C:\images\three.webp", "three.webp", [3]),
                ],
                3,
                []));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, useCase);

        await sut.AddDroppedFilesAsync([@"C:\images\one.jpg"], TestContext.Current.CancellationToken);
        await sut.AddDroppedFilesAsync(
            [@"C:\images\two.png", @"C:\images\three.webp"],
            TestContext.Current.CancellationToken);

        Assert.Equal(["one.jpg", "two.png", "three.webp"], view.AppendedImages.Select(image => image.FileName));
        Assert.Equal(3, view.DraftCount);
    }

    [Theory]
    [InlineData(DraftImageRejectionReason.UnsupportedFormat, "対応していない形式です。")]
    [InlineData(DraftImageRejectionReason.FileTooLarge, "ファイルサイズが10MBを超えています。")]
    [InlineData(DraftImageRejectionReason.DraftLimitReached, "Draftは最大20枚です。")]
    [InlineData(DraftImageRejectionReason.UnreadableImage, "画像を読み込めません。")]
    [InlineData(DraftImageRejectionReason.FileFormatMismatch, "拡張子と画像形式が一致しません。")]
    public async Task AddDroppedFilesAsync_WithRejection_DisplaysFileNameAndFriendlyReason(
        DraftImageRejectionReason reason,
        string expectedMessage)
    {
        var rejection = new RejectedDraftImage(@"C:\images\invalid.jpg", "invalid.jpg", reason);
        var useCase = new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [rejection]));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, useCase);

        await sut.AddDroppedFilesAsync([rejection.FilePath], TestContext.Current.CancellationToken);

        var displayed = Assert.Single(view.RejectedImages);
        Assert.Equal("invalid.jpg", displayed.FileName);
        Assert.Equal(expectedMessage, displayed.Message);
    }

    private sealed class StubAddImagesToDraftUseCase(params AddImagesToDraftResult[] results)
        : IAddImagesToDraftUseCase
    {
        private readonly Queue<AddImagesToDraftResult> _results = new(results);

        public Task<AddImagesToDraftResult> ExecuteAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken) =>
            Task.FromResult(_results.Dequeue());
    }

    private sealed class FakeMainView : IMainView
    {
        public List<DraftImageViewModel> AppendedImages { get; } = [];

        public List<bool> DropEnabledChanges { get; } = [];

        public int DraftCount { get; private set; }
        public List<RejectedImageViewModel> RejectedImages { get; } = [];

        public void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images) =>
            AppendedImages.AddRange(images);

        public void SetDraftCount(int count) => DraftCount = count;

        public void SetDropEnabled(bool enabled) => DropEnabledChanges.Add(enabled);

        public void DisplayRejectedImages(IReadOnlyCollection<RejectedImageViewModel> images)
        {
            RejectedImages.Clear();
            RejectedImages.AddRange(images);
        }
    }
}

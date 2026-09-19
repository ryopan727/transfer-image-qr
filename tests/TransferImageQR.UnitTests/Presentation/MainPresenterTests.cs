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
        var sut = new MainPresenter(view, useCase, new StubEditDraftUseCase());

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
        var sut = new MainPresenter(view, useCase, new StubEditDraftUseCase());

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
        var sut = new MainPresenter(view, useCase, new StubEditDraftUseCase());

        await sut.AddDroppedFilesAsync([rejection.FilePath], TestContext.Current.CancellationToken);

        var displayed = Assert.Single(view.RejectedImages);
        Assert.Equal("invalid.jpg", displayed.FileName);
        Assert.Equal(expectedMessage, displayed.Message);
    }

    [Fact]
    public async Task RemoveDraftImage_WithExistingImage_RemovesItAndUpdatesActions()
    {
        var imageId = Guid.NewGuid();
        var addUseCase = new StubAddImagesToDraftUseCase(
            new AddImagesToDraftResult(
                [new DraftImageListItem(imageId, @"C:\images\one.jpg", "one.jpg", [1])],
                1,
                []));
        var editUseCase = new StubEditDraftUseCase(
            removeResult: new DraftEditResult(true, 0));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, addUseCase, editUseCase);
        await sut.AddDroppedFilesAsync([@"C:\images\one.jpg"], TestContext.Current.CancellationToken);

        sut.RemoveDraftImage(imageId);

        Assert.Equal(imageId, editUseCase.RemovedImageId);
        Assert.Equal([imageId], view.RemovedImageIds);
        Assert.Equal(0, view.DraftCount);
        Assert.False(view.DraftActionsEnabled);
    }

    [Fact]
    public async Task ClearDraft_WithImages_ClearsViewAndDisablesActions()
    {
        var addUseCase = new StubAddImagesToDraftUseCase(
            new AddImagesToDraftResult(
                [new DraftImageListItem(Guid.NewGuid(), @"C:\images\one.jpg", "one.jpg", [1])],
                1,
                []));
        var editUseCase = new StubEditDraftUseCase(clearResult: new DraftEditResult(true, 0));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, addUseCase, editUseCase);
        await sut.AddDroppedFilesAsync([@"C:\images\one.jpg"], TestContext.Current.CancellationToken);

        sut.ClearDraft();

        Assert.True(editUseCase.ClearCalled);
        Assert.True(view.DraftCleared);
        Assert.Equal(0, view.DraftCount);
        Assert.False(view.DraftActionsEnabled);
    }

    [Fact]
    public void SetDraftEditingEnabled_WhenDisabled_DisablesDropAndDraftActions()
    {
        var view = new FakeMainView();
        var sut = new MainPresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase());

        sut.SetDraftEditingEnabled(false);

        Assert.False(view.DraftEditingEnabled);
        Assert.False(Assert.Single(view.DropEnabledChanges));
    }

    [Fact]
    public async Task DraftEditingCommands_WhenEditingIsDisabled_DoNotChangeDraft()
    {
        var addUseCase = new StubAddImagesToDraftUseCase(
            new AddImagesToDraftResult([], 1, []));
        var editUseCase = new StubEditDraftUseCase(
            removeResult: new DraftEditResult(true, 0),
            clearResult: new DraftEditResult(true, 0));
        var view = new FakeMainView();
        var sut = new MainPresenter(view, addUseCase, editUseCase);
        sut.SetDraftEditingEnabled(false);

        await sut.AddDroppedFilesAsync([@"C:\images\blocked.jpg"], TestContext.Current.CancellationToken);
        sut.RemoveDraftImage(Guid.NewGuid());
        sut.ClearDraft();

        Assert.Null(editUseCase.RemovedImageId);
        Assert.False(editUseCase.ClearCalled);
        Assert.Empty(view.AppendedImages);
        Assert.Empty(view.RemovedImageIds);
        Assert.False(view.DraftCleared);
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
        public List<Guid> RemovedImageIds { get; } = [];
        public bool DraftCleared { get; private set; }
        public bool DraftActionsEnabled { get; private set; }
        public bool DraftEditingEnabled { get; private set; } = true;

        public void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images) =>
            AppendedImages.AddRange(images);

        public void SetDraftCount(int count) => DraftCount = count;

        public void SetDropEnabled(bool enabled) => DropEnabledChanges.Add(enabled);

        public void RemoveDraftImage(Guid imageId) => RemovedImageIds.Add(imageId);

        public void ClearDraftImages() => DraftCleared = true;

        public void SetDraftActionsEnabled(bool enabled) => DraftActionsEnabled = enabled;

        public void SetDraftEditingEnabled(bool enabled) => DraftEditingEnabled = enabled;

        public void DisplayRejectedImages(IReadOnlyCollection<RejectedImageViewModel> images)
        {
            RejectedImages.Clear();
            RejectedImages.AddRange(images);
        }
    }

    private sealed class StubEditDraftUseCase(
        DraftEditResult? removeResult = null,
        DraftEditResult? clearResult = null) : IEditDraftUseCase
    {
        public Guid? RemovedImageId { get; private set; }
        public bool ClearCalled { get; private set; }

        public DraftEditResult Remove(Guid imageId)
        {
            RemovedImageId = imageId;
            return removeResult ?? new DraftEditResult(false, 0);
        }

        public DraftEditResult Clear()
        {
            ClearCalled = true;
            return clearResult ?? new DraftEditResult(false, 0);
        }
    }
}

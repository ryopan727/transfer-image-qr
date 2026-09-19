using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
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
        var sut = CreatePresenter(view, useCase, new StubEditDraftUseCase(), new StubTransferSessionUseCase());

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
        var sut = CreatePresenter(view, useCase, new StubEditDraftUseCase(), new StubTransferSessionUseCase());

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
    [InlineData(DraftImageRejectionReason.DraftNotEditable, "転送中はDraftを変更できません。")]
    public async Task AddDroppedFilesAsync_WithRejection_DisplaysFileNameAndFriendlyReason(
        DraftImageRejectionReason reason,
        string expectedMessage)
    {
        var rejection = new RejectedDraftImage(@"C:\images\invalid.jpg", "invalid.jpg", reason);
        var useCase = new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [rejection]));
        var view = new FakeMainView();
        var sut = CreatePresenter(view, useCase, new StubEditDraftUseCase(), new StubTransferSessionUseCase());

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
        var sut = CreatePresenter(view, addUseCase, editUseCase, new StubTransferSessionUseCase());
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
        var sut = CreatePresenter(view, addUseCase, editUseCase, new StubTransferSessionUseCase());
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
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase());

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
        var sut = CreatePresenter(view, addUseCase, editUseCase, new StubTransferSessionUseCase());
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

    [Fact]
    public void CreateTransferSession_WhenSuccessful_DisablesEditingAndDisplaysActiveState()
    {
        var expiresAt = new DateTimeOffset(2026, 9, 20, 12, 5, 0, TimeSpan.Zero);
        var sessionUseCase = new StubTransferSessionUseCase(
            createResult: CreateSuccessfulSession(),
            status: new TransferSessionStatus(TransferSessionState.Active, expiresAt));
        var view = new FakeMainView();
        var qrCode = new TransferQrCode(
            new Uri("http://192.168.1.20:51846/transfer/token"),
            [1, 2, 3]);
        var qrCodeService = new StubTransferQrCodeService(qrCode);
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            sessionUseCase,
            qrCodeService);

        sut.CreateTransferSession();

        Assert.True(sessionUseCase.CreateCalled);
        Assert.False(view.DraftEditingEnabled);
        Assert.NotNull(view.TransferSession);
        Assert.False(view.TransferSession.IsExpired);
        Assert.Equal(expiresAt, view.TransferSession.ExpiresAt);
        Assert.Equal("token", qrCodeService.SessionToken);
        Assert.Equal(qrCode.Url.AbsoluteUri, view.TransferSession.TransferUrl);
        Assert.Equal(qrCode.PngBytes, view.TransferSession.QrCodePng);
    }

    [Fact]
    public async Task CreateTransferSession_WhileImagesAreBeingAdded_DoesNotConfirmDraft()
    {
        var addUseCase = new PendingAddImagesToDraftUseCase();
        var sessionUseCase = new StubTransferSessionUseCase(
            createResult: CreateSuccessfulSession(),
            status: new TransferSessionStatus(
                TransferSessionState.Active,
                new DateTimeOffset(2026, 9, 20, 12, 5, 0, TimeSpan.Zero)));
        var sut = CreatePresenter(
            new FakeMainView(),
            addUseCase,
            new StubEditDraftUseCase(),
            sessionUseCase);
        var adding = sut.AddDroppedFilesAsync(
            [@"C:\images\pending.jpg"],
            TestContext.Current.CancellationToken);

        sut.CreateTransferSession();
        addUseCase.Complete(new AddImagesToDraftResult([], 0, []));
        await adding;

        Assert.False(sessionUseCase.CreateCalled);
    }

    [Fact]
    public void RefreshTransferSessionState_AfterExpiration_DisplaysExpiredState()
    {
        var expiresAt = new DateTimeOffset(2026, 9, 20, 12, 5, 0, TimeSpan.Zero);
        var sessionUseCase = new StubTransferSessionUseCase(
            status: new TransferSessionStatus(TransferSessionState.Expired, expiresAt));
        var view = new FakeMainView();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            sessionUseCase);

        sut.RefreshTransferSessionState();

        Assert.True(view.TransferSession?.IsExpired);
    }

    [Fact]
    public void StartNewTransfer_ResetsViewToEmptyEditableDraft()
    {
        var sessionUseCase = new StubTransferSessionUseCase();
        var view = new FakeMainView();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            sessionUseCase);
        sut.SetDraftEditingEnabled(false);

        sut.StartNewTransfer();

        Assert.True(sessionUseCase.StartNewTransferCalled);
        Assert.True(view.DraftCleared);
        Assert.Equal(0, view.DraftCount);
        Assert.True(view.DraftEditingEnabled);
        Assert.True(view.DraftStateDisplayed);
    }

    private static CreateTransferSessionResult CreateSuccessfulSession()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\one.jpg");
        return new CreateTransferSessionResult(
            true,
            new TransferSession(
                "token",
                draft.Images,
                new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero)));
    }

    private static MainPresenter CreatePresenter(
        IMainView view,
        IAddImagesToDraftUseCase addUseCase,
        IEditDraftUseCase editUseCase,
        ITransferSessionUseCase sessionUseCase,
        ITransferQrCodeService? qrCodeService = null) =>
        new(
            view,
            addUseCase,
            editUseCase,
            sessionUseCase,
            qrCodeService ?? new StubTransferQrCodeService());

    private sealed class StubAddImagesToDraftUseCase(params AddImagesToDraftResult[] results)
        : IAddImagesToDraftUseCase
    {
        private readonly Queue<AddImagesToDraftResult> _results = new(results);

        public Task<AddImagesToDraftResult> ExecuteAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken) =>
            Task.FromResult(_results.Dequeue());
    }

    private sealed class PendingAddImagesToDraftUseCase : IAddImagesToDraftUseCase
    {
        private readonly TaskCompletionSource<AddImagesToDraftResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<AddImagesToDraftResult> ExecuteAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken) => _completion.Task;

        public void Complete(AddImagesToDraftResult result) => _completion.SetResult(result);
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
        public TransferSessionViewModel? TransferSession { get; private set; }
        public bool DraftStateDisplayed { get; private set; }

        public void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images) =>
            AppendedImages.AddRange(images);

        public void SetDraftCount(int count) => DraftCount = count;

        public void SetDropEnabled(bool enabled) => DropEnabledChanges.Add(enabled);

        public void RemoveDraftImage(Guid imageId) => RemovedImageIds.Add(imageId);

        public void ClearDraftImages() => DraftCleared = true;

        public void SetDraftActionsEnabled(bool enabled) => DraftActionsEnabled = enabled;

        public void SetDraftEditingEnabled(bool enabled) => DraftEditingEnabled = enabled;

        public void DisplayTransferSession(TransferSessionViewModel session) => TransferSession = session;

        public void DisplayDraftState() => DraftStateDisplayed = true;

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

    private sealed class StubTransferSessionUseCase(
        CreateTransferSessionResult? createResult = null,
        TransferSessionStatus? status = null) : ITransferSessionUseCase
    {
        public bool CreateCalled { get; private set; }
        public bool StartNewTransferCalled { get; private set; }

        public CreateTransferSessionResult Create()
        {
            CreateCalled = true;
            return createResult ?? new CreateTransferSessionResult(false, null);
        }

        public TransferSession? GetCurrent() => createResult?.Session;

        public TransferSessionStatus? GetCurrentStatus() => status;

        public void StartNewTransfer() => StartNewTransferCalled = true;
    }

    private sealed class StubTransferQrCodeService(TransferQrCode? result = null)
        : ITransferQrCodeService
    {
        public string? SessionToken { get; private set; }

        public TransferQrCode? Create(string sessionToken)
        {
            SessionToken = sessionToken;
            return result;
        }
    }
}

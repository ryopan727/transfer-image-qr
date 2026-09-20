using System.Net;
using TransferImageQR.Application.Backgrounds;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Application.Tray;
using TransferImageQR.Application.AutoStart;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class MainPresenterTests
{
    [Fact]
    public void AutoStartSettings_LoadAndChangeUpdateViewAndRegistration()
    {
        var view = new FakeMainView();
        var autoStart = new StubAutoStartSettingsUseCase(true);
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            autoStartSettingsUseCase: autoStart);

        sut.LoadAutoStartSettings();
        sut.SetAutoStartEnabled(false);

        Assert.False(view.AutoStartEnabled);
        Assert.False(autoStart.SavedValue);
        Assert.Null(view.AutoStartError);
    }

    [Fact]
    public void AutoStartSettings_WhenRegistrationFails_RestoresStateAndDisplaysFriendlyError()
    {
        var view = new FakeMainView();
        var autoStart = new StubAutoStartSettingsUseCase(
            true,
            AutoStartSettingsError.RegistrationUnavailable);
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            autoStartSettingsUseCase: autoStart);

        sut.SetAutoStartEnabled(false);

        Assert.True(view.AutoStartEnabled);
        Assert.Equal("自動起動設定を変更できません。", view.AutoStartError);
    }

    [Fact]
    public void TraySettings_ControlCloseShowAndExitBehavior()
    {
        var view = new FakeMainView();
        var tray = new StubTraySettingsUseCase(true);
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            traySettingsUseCase: tray);

        sut.LoadTraySettings();
        var closeWasHandled = sut.RequestWindowClose();
        sut.ShowMainWindow();
        sut.ExitApplication();
        sut.SetMinimizeToTray(false);

        Assert.True(closeWasHandled);
        Assert.True(view.HiddenToTray);
        Assert.True(view.ShownFromTray);
        Assert.True(view.ExitRequested);
        Assert.False(view.TrayModeEnabled);
        Assert.False(sut.RequestWindowClose());
        Assert.False(tray.SavedValue);
    }

    [Fact]
    public void TraySettings_WhenSaveFails_DisplaysFriendlyError()
    {
        var view = new FakeMainView();
        var tray = new StubTraySettingsUseCase(false, TraySettingsError.SettingsCouldNotBeSaved);
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            traySettingsUseCase: tray);

        sut.SetMinimizeToTray(true);

        Assert.Equal("常駐設定を保存できません。", view.TraySettingsError);
    }

    [Fact]
    public void BackgroundCommands_ApplyUseCaseResultsAndFriendlyErrors()
    {
        var view = new FakeMainView();
        var background = new StubBackgroundCustomizationUseCase();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            backgroundCustomizationUseCase: background);

        sut.LoadBackground();
        sut.SelectBackgroundImage("oshi.png");
        sut.UpdateBackgroundAppearance(60, 150, 10, -20);
        sut.ClearBackground();

        Assert.Equal(4, view.Backgrounds.Count);
        Assert.Equal("oshi.png", background.SelectedPath);
        Assert.Equal((60, 150, 10, -20), background.Appearance);
        Assert.True(background.Cleared);
        Assert.Contains("読み込めません", view.BackgroundError);
    }

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
        var lanAddressProvider = new StubLanAddressProvider(
            new LanAddressOption(IPAddress.Parse("192.168.1.20"), "Ethernet"));
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            sessionUseCase,
            qrCodeService,
            lanAddressProvider);
        sut.Initialize();

        sut.CreateTransferSession();

        Assert.True(sessionUseCase.CreateCalled);
        Assert.False(view.DraftEditingEnabled);
        Assert.NotNull(view.TransferSession);
        Assert.False(view.TransferSession.IsExpired);
        Assert.Equal(expiresAt, view.TransferSession.ExpiresAt);
        Assert.Equal("token", qrCodeService.SessionToken);
        Assert.Equal(IPAddress.Parse("192.168.1.20"), qrCodeService.Address);
        Assert.Equal(qrCode.Url.AbsoluteUri, view.TransferSession.TransferUrl);
        Assert.Equal(qrCode.PngBytes, view.TransferSession.QrCodePng);
    }

    [Fact]
    public void Initialize_WithMultipleLanAddresses_DisplaysCandidatesAndSelectsFirst()
    {
        var view = new FakeMainView();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            lanAddressProvider: new StubLanAddressProvider(
                new LanAddressOption(IPAddress.Parse("192.168.1.20"), "Ethernet"),
                new LanAddressOption(IPAddress.Parse("192.168.1.50"), "Wi-Fi")));

        sut.Initialize();

        Assert.Equal(
            ["Ethernet — 192.168.1.20", "Wi-Fi — 192.168.1.50"],
            view.LanAddresses.Select(address => address.DisplayName));
        Assert.Equal("192.168.1.20", view.SelectedLanAddress);
        Assert.True(view.LanAddressSelectionEnabled);
    }

    [Fact]
    public void SelectedLanAddress_IsUsedForQrAndRetainedAfterStartingNewTransfer()
    {
        var view = new FakeMainView();
        var qrCodeService = new StubTransferQrCodeService();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(createResult: CreateSuccessfulSession()),
            qrCodeService,
            new StubLanAddressProvider(
                new LanAddressOption(IPAddress.Parse("192.168.1.20"), "Ethernet"),
                new LanAddressOption(IPAddress.Parse("192.168.1.50"), "Wi-Fi")));
        sut.Initialize();

        sut.SelectLanAddress("192.168.1.50");
        sut.CreateTransferSession();

        Assert.Equal(IPAddress.Parse("192.168.1.50"), qrCodeService.Address);
        Assert.False(view.LanAddressSelectionEnabled);

        sut.SelectLanAddress("192.168.1.20");
        sut.StartNewTransfer();

        Assert.Equal("192.168.1.50", view.SelectedLanAddress);
        Assert.True(view.LanAddressSelectionEnabled);
    }

    [Fact]
    public void Initialize_WithoutLanAddresses_DisplaysDisabledEmptySelection()
    {
        var view = new FakeMainView();
        var sut = CreatePresenter(
            view,
            new StubAddImagesToDraftUseCase(),
            new StubEditDraftUseCase(),
            new StubTransferSessionUseCase(),
            lanAddressProvider: new StubLanAddressProvider());

        sut.Initialize();

        Assert.Empty(view.LanAddresses);
        Assert.Null(view.SelectedLanAddress);
        Assert.False(view.LanAddressSelectionEnabled);
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
        ITransferQrCodeService? qrCodeService = null,
        ILanAddressProvider? lanAddressProvider = null,
        IBackgroundCustomizationUseCase? backgroundCustomizationUseCase = null,
        ITraySettingsUseCase? traySettingsUseCase = null,
        IAutoStartSettingsUseCase? autoStartSettingsUseCase = null) =>
        new(
            view,
            addUseCase,
            editUseCase,
            sessionUseCase,
            qrCodeService ?? new StubTransferQrCodeService(),
            lanAddressProvider ?? new StubLanAddressProvider(),
            backgroundCustomizationUseCase,
            traySettingsUseCase,
            autoStartSettingsUseCase);

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
        public IReadOnlyCollection<LanAddressViewModel> LanAddresses { get; private set; } = [];
        public string? SelectedLanAddress { get; private set; }
        public bool LanAddressSelectionEnabled { get; private set; }
        public List<BackgroundViewModel> Backgrounds { get; } = [];
        public string? BackgroundError { get; private set; }
        public bool TrayModeEnabled { get; private set; }
        public string? TraySettingsError { get; private set; }
        public bool HiddenToTray { get; private set; }
        public bool ShownFromTray { get; private set; }
        public bool ExitRequested { get; private set; }
        public bool AutoStartEnabled { get; private set; }
        public string? AutoStartError { get; private set; }

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

        public void DisplayLanAddresses(
            IReadOnlyCollection<LanAddressViewModel> addresses,
            string? selectedAddress)
        {
            LanAddresses = addresses;
            SelectedLanAddress = selectedAddress;
        }

        public void SetLanAddressSelectionEnabled(bool enabled) =>
            LanAddressSelectionEnabled = enabled;

        public void ApplyBackground(BackgroundViewModel background) => Backgrounds.Add(background);

        public void DisplayBackgroundError(string? message) => BackgroundError = message;

        public void SetTrayMode(bool enabled) => TrayModeEnabled = enabled;

        public void DisplayTraySettingsError(string? message) => TraySettingsError = message;

        public void SetAutoStartMode(bool enabled) => AutoStartEnabled = enabled;

        public void DisplayAutoStartError(string? message) => AutoStartError = message;

        public void HideToTray() => HiddenToTray = true;

        public void ShowFromTray() => ShownFromTray = true;

        public void ExitApplication() => ExitRequested = true;

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
        public IPAddress? Address { get; private set; }

        public TransferQrCode? Create(string sessionToken, IPAddress? address)
        {
            SessionToken = sessionToken;
            Address = address;
            return result;
        }
    }

    private sealed class StubLanAddressProvider(params LanAddressOption[] options)
        : ILanAddressProvider
    {
        public IReadOnlyList<LanAddressOption> GetIPv4Addresses() => options;
    }

    private sealed class StubBackgroundCustomizationUseCase : IBackgroundCustomizationUseCase
    {
        public string? SelectedPath { get; private set; }
        public (int Opacity, int Zoom, int X, int Y)? Appearance { get; private set; }
        public bool Cleared { get; private set; }

        public BackgroundCustomizationResult Load() => Result();

        public BackgroundCustomizationResult SelectImage(string filePath)
        {
            SelectedPath = filePath;
            return Result();
        }

        public BackgroundCustomizationResult UpdateAppearance(
            int opacityPercent,
            int zoomPercent,
            int offsetX,
            int offsetY)
        {
            Appearance = (opacityPercent, zoomPercent, offsetX, offsetY);
            return Result();
        }

        public BackgroundCustomizationResult Clear()
        {
            Cleared = true;
            return Result(BackgroundCustomizationError.ImageUnreadable);
        }

        private static BackgroundCustomizationResult Result(
            BackgroundCustomizationError error = BackgroundCustomizationError.None) =>
            new(BackgroundSettings.Default, null, error);
    }

    private sealed class StubTraySettingsUseCase(
        bool initialValue,
        TraySettingsError saveError = TraySettingsError.None) : ITraySettingsUseCase
    {
        public bool? SavedValue { get; private set; }

        public TraySettingsResult Load() => new(new TraySettings(initialValue));

        public TraySettingsResult SetMinimizeToTray(bool enabled)
        {
            SavedValue = enabled;
            return new TraySettingsResult(new TraySettings(enabled), saveError);
        }
    }

    private sealed class StubAutoStartSettingsUseCase(
        bool currentValue,
        AutoStartSettingsError error = AutoStartSettingsError.None) : IAutoStartSettingsUseCase
    {
        public bool? SavedValue { get; private set; }

        public AutoStartSettingsResult Load() => new(currentValue, error);

        public AutoStartSettingsResult SetEnabled(bool enabled)
        {
            SavedValue = enabled;
            return new AutoStartSettingsResult(error == AutoStartSettingsError.None ? enabled : currentValue, error);
        }
    }
}

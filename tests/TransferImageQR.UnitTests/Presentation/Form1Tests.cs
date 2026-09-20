using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using SkiaSharp;
using TransferImageQR.Application.Backgrounds;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Application.Tray;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class Form1Tests
{
    [Fact]
    public void TrayMode_CloseHidesWindowAndMenuCanShowAndExit()
    {
        RunInSta(() =>
        {
            var tray = new StubTraySettingsUseCase(true);
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [])),
                new StubEditDraftUseCase(),
                new StubTransferSessionUseCase(),
                new StubTransferQrCodeService(),
                traySettingsUseCase: tray);
            form.AttachPresenter(presenter);
            presenter.LoadTraySettings();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var checkBox = Assert.IsType<CheckBox>(
                Assert.Single(form.Controls.Find("minimizeToTrayCheckBox", true)));
            var trayIcon = GetTrayIcon(form);
            Assert.True(checkBox.Checked);
            Assert.True(trayIcon.Visible);

            checkBox.Checked = false;
            checkBox.Checked = true;
            Assert.True(tray.SavedValue);

            form.Close();
            System.Windows.Forms.Application.DoEvents();
            Assert.False(form.Visible);
            Assert.False(form.IsDisposed);

            Assert.NotNull(trayIcon.ContextMenuStrip);
            trayIcon.ContextMenuStrip.Items["showMainWindowMenuItem"]?.PerformClick();
            System.Windows.Forms.Application.DoEvents();
            Assert.True(form.Visible);

            trayIcon.ContextMenuStrip.Items["exitApplicationMenuItem"]?.PerformClick();
            System.Windows.Forms.Application.DoEvents();
            Assert.True(form.IsDisposed);
        });
    }

    [Fact]
    public void TrayMode_WhenDisabled_CloseDisposesWindow()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [])),
                new StubEditDraftUseCase(),
                new StubTransferSessionUseCase(),
                new StubTransferQrCodeService(),
                traySettingsUseCase: new StubTraySettingsUseCase(false));
            form.AttachPresenter(presenter);
            presenter.LoadTraySettings();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            form.Close();
            System.Windows.Forms.Application.DoEvents();

            Assert.True(form.IsDisposed);
        });
    }

    [Fact]
    public void BackgroundControls_ExposeAccessibleAdjustmentAndClearActions()
    {
        RunInSta(() =>
        {
            var background = new StubBackgroundCustomizationUseCase();
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [])),
                new StubEditDraftUseCase(),
                new StubTransferSessionUseCase(),
                new StubTransferQrCodeService(),
                backgroundCustomizationUseCase: background);
            form.AttachPresenter(presenter);
            form.ApplyBackground(new BackgroundViewModel(CreateTransparentPng(), 40, 125, 5, -10));
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var group = Assert.IsType<GroupBox>(Assert.Single(form.Controls.Find("backgroundSettingsGroup", true)));
            var opacity = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundOpacityInput", true)));
            var zoom = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundZoomInput", true)));
            var offsetX = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundOffsetXInput", true)));
            var clear = Assert.IsType<Button>(Assert.Single(form.Controls.Find("clearBackgroundButton", true)));

            Assert.Equal("背景設定", group.AccessibleName);
            Assert.Equal(40, opacity.Value);
            Assert.Equal(125, zoom.Value);
            Assert.Equal(5, offsetX.Value);
            Assert.True(clear.Enabled);
            clear.PerformClick();
            opacity.Value = 65;

            Assert.Equal(65, background.Appearance?.Opacity);
            Assert.True(background.Cleared);
        });
    }

    [Fact]
    public void BackgroundRendering_PreservesSourceAlphaAndKeepsForegroundPanelsOpaque()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            form.ApplyBackground(new BackgroundViewModel(CreateTransparentPng(), 100, 300, 0, 0));
            form.Show();
            System.Windows.Forms.Application.DoEvents();
            using var rendered = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);

            form.DrawToBitmap(rendered, form.ClientRectangle);

            var backgroundPixel = rendered.GetPixel(10, 300);
            Assert.InRange(backgroundPixel.R, 245, 255);
            Assert.InRange(backgroundPixel.G, 115, 140);
            Assert.InRange(backgroundPixel.B, 115, 140);
            var dropPanel = Assert.IsType<Panel>(Assert.Single(form.Controls.Find("dropPanel", true)));
            Assert.Equal(Color.FromArgb(245, 248, 252), dropPanel.BackColor);
            var qrPanel = Assert.IsType<Panel>(Assert.Single(form.Controls.Find("qrPanel", true)));
            Assert.Equal(Color.White, qrPanel.BackColor);
        });
    }

    [Fact]
    public void MainForm_ExposesEnabledFileDropAreaAndEmptyDraftState()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var dropPanel = Assert.IsType<Panel>(Assert.Single(form.Controls.Find("dropPanel", true)));
            var draftList = Assert.IsType<ListView>(Assert.Single(form.Controls.Find("draftListView", true)));
            var emptyLabel = Assert.IsType<Label>(Assert.Single(form.Controls.Find("emptyDraftLabel", true)));

            Assert.True(dropPanel.AllowDrop);
            Assert.Equal("Draft画像一覧", draftList.AccessibleName);
            Assert.True(emptyLabel.Visible);
        });
    }

    [Fact]
    public void PresenterResult_RendersThumbnailFileNameAndCount()
    {
        RunInSta(() =>
        {
            var image = new DraftImageListItem(
                Guid.NewGuid(),
                @"C:\images\sample.webp",
                "sample.webp",
                CreatePng());
            var useCase = new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([image], 1, []));
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                useCase,
                new StubEditDraftUseCase(),
                new StubTransferSessionUseCase(),
                new StubTransferQrCodeService());
            form.AttachPresenter(presenter);

            presenter.AddDroppedFilesAsync([image.FilePath]).GetAwaiter().GetResult();

            var draftList = Assert.IsType<ListView>(Assert.Single(form.Controls.Find("draftListView", true)));
            var countLabel = Assert.IsType<Label>(Assert.Single(form.Controls.Find("draftCountLabel", true)));
            var item = Assert.Single(draftList.Items.Cast<ListViewItem>());
            var imageList = Assert.IsType<ImageList>(draftList.LargeImageList);
            Assert.Equal("sample.webp", item.Text);
            Assert.Equal("Draft: 1枚", countLabel.Text);
            Assert.NotEmpty(item.ImageKey);
            Assert.True(imageList.Images.ContainsKey(item.ImageKey));
        });
    }

    [Fact]
    public void DraftActions_ReflectImageCountAndEditingState()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var clearButton = Assert.IsType<Button>(Assert.Single(form.Controls.Find("clearDraftButton", true)));
            var createQrButton = Assert.IsType<Button>(Assert.Single(form.Controls.Find("createQrButton", true)));
            Assert.False(clearButton.Enabled);
            Assert.False(createQrButton.Enabled);

            form.SetDraftActionsEnabled(true);
            Assert.True(clearButton.Enabled);
            Assert.True(createQrButton.Enabled);

            form.SetDraftEditingEnabled(false);
            Assert.False(clearButton.Enabled);
            Assert.False(createQrButton.Enabled);
            var dropPanel = Assert.IsType<Panel>(Assert.Single(form.Controls.Find("dropPanel", true)));
            var instruction = Assert.IsType<Label>(Assert.Single(form.Controls.Find("dropInstructionLabel", true)));
            Assert.False(dropPanel.AllowDrop);
            Assert.Equal("転送中は画像を変更できません", instruction.Text);
        });
    }

    [Fact]
    public void RemoveAndClearDraftImages_UpdateListAndDisposeThumbnails()
    {
        RunInSta(() =>
        {
            var first = new DraftImageViewModel(Guid.NewGuid(), @"C:\images\first.jpg", "first.jpg", CreatePng());
            var second = new DraftImageViewModel(Guid.NewGuid(), @"C:\images\second.png", "second.png", CreatePng());
            using var form = new Form1();
            form.AppendDraftImages([first, second]);

            form.RemoveDraftImage(first.Id);

            var draftList = Assert.IsType<ListView>(Assert.Single(form.Controls.Find("draftListView", true)));
            Assert.Equal("second.png", Assert.Single(draftList.Items.Cast<ListViewItem>()).Text);
            Assert.False(Assert.IsType<ImageList>(draftList.LargeImageList).Images.ContainsKey(first.Id.ToString("N")));

            form.ClearDraftImages();
            Assert.Empty(draftList.Items.Cast<ListViewItem>());
            Assert.Empty(Assert.IsType<ImageList>(draftList.LargeImageList).Images.Cast<Image>());
        });
    }

    [Fact]
    public void TransferSessionState_ShowsActiveExpiredAndNewTransferActions()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            form.Show();
            System.Windows.Forms.Application.DoEvents();
            var expiresAt = new DateTimeOffset(2026, 9, 20, 12, 5, 0, TimeSpan.Zero);

            form.DisplayTransferSession(new TransferSessionViewModel(false, expiresAt));

            var stateLabel = Assert.IsType<Label>(Assert.Single(form.Controls.Find("sessionStateLabel", true)));
            var newTransferButton = Assert.IsType<Button>(Assert.Single(form.Controls.Find("newTransferButton", true)));
            var qrPicture = Assert.IsType<PictureBox>(Assert.Single(form.Controls.Find("qrPictureBox", true)));
            var qrStatus = Assert.IsType<Label>(Assert.Single(form.Controls.Find("qrStatusLabel", true)));
            Assert.Contains("Active", stateLabel.Text);
            Assert.True(newTransferButton.Visible);
            Assert.False(qrPicture.Visible);
            Assert.True(qrStatus.Visible);
            Assert.Contains("LAN用IPv4", qrStatus.Text);

            form.DisplayTransferSession(new TransferSessionViewModel(true, expiresAt));
            Assert.Contains("Expired", stateLabel.Text);

            form.DisplayDraftState();
            Assert.Equal("状態: Draft", stateLabel.Text);
            Assert.False(newTransferButton.Visible);
        });
    }

    [Fact]
    public void TransferSessionQr_RendersUrlAndReleasesImageWhenReturningToDraft()
    {
        RunInSta(() =>
        {
            using var form = new Form1();
            form.Show();
            System.Windows.Forms.Application.DoEvents();
            var expiresAt = new DateTimeOffset(2026, 9, 20, 12, 5, 0, TimeSpan.Zero);
            const string url = "http://192.168.1.20:51846/transfer/current-token";

            form.DisplayTransferSession(new TransferSessionViewModel(false, expiresAt, url, CreatePng()));

            var qrPanel = Assert.IsType<Panel>(Assert.Single(form.Controls.Find("qrPanel", true)));
            var qrPicture = Assert.IsType<PictureBox>(Assert.Single(form.Controls.Find("qrPictureBox", true)));
            var urlText = Assert.IsType<TextBox>(Assert.Single(form.Controls.Find("transferUrlTextBox", true)));
            Assert.True(qrPanel.Visible);
            Assert.True(qrPicture.Visible);
            Assert.NotNull(qrPicture.Image);
            Assert.Equal(url, urlText.Text);

            form.DisplayTransferSession(new TransferSessionViewModel(true, expiresAt, url, CreatePng()));
            var qrStatus = Assert.IsType<Label>(Assert.Single(form.Controls.Find("qrStatusLabel", true)));
            Assert.False(qrPicture.Visible);
            Assert.Contains("有効期限", qrStatus.Text);

            form.DisplayDraftState();
            Assert.False(qrPanel.Visible);
            Assert.Null(qrPicture.Image);
        });
    }

    [Fact]
    public void SessionButtons_DelegateCreateAndStartNewTransferToPresenter()
    {
        RunInSta(() =>
        {
            var sessionUseCase = new StubTransferSessionUseCase(CreateSuccessfulSession());
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 1, [])),
                new StubEditDraftUseCase(),
                sessionUseCase,
                new StubTransferQrCodeService(),
                new StubLanAddressProvider());
            form.AttachPresenter(presenter);
            form.Show();
            form.SetDraftActionsEnabled(true);
            System.Windows.Forms.Application.DoEvents();

            var createButton = Assert.IsType<Button>(Assert.Single(form.Controls.Find("createQrButton", true)));
            createButton.PerformClick();

            Assert.True(sessionUseCase.CreateCalled);
            var newTransferButton = Assert.IsType<Button>(Assert.Single(form.Controls.Find("newTransferButton", true)));
            Assert.True(newTransferButton.Visible);
            newTransferButton.PerformClick();
            Assert.True(sessionUseCase.StartNewTransferCalled);
        });
    }

    [Fact]
    public void LanAddressSelection_RendersCandidatesAndDelegatesSelection()
    {
        RunInSta(() =>
        {
            var qrCodeService = new StubTransferQrCodeService();
            using var form = new Form1();
            var presenter = new MainPresenter(
                form,
                new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([], 0, [])),
                new StubEditDraftUseCase(),
                new StubTransferSessionUseCase(CreateSuccessfulSession()),
                qrCodeService,
                new StubLanAddressProvider(
                    new LanAddressOption(IPAddress.Parse("192.168.1.20"), "Ethernet"),
                    new LanAddressOption(IPAddress.Parse("192.168.1.50"), "Wi-Fi")));
            form.AttachPresenter(presenter);
            presenter.Initialize();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var comboBox = Assert.IsType<ComboBox>(
                Assert.Single(form.Controls.Find("lanAddressComboBox", true)));
            Assert.Equal("転送に使うLANアドレス", comboBox.AccessibleName);
            Assert.Equal(2, comboBox.Items.Count);
            Assert.Equal("Ethernet — 192.168.1.20", comboBox.Text);

            comboBox.SelectedIndex = 1;
            form.SetDraftActionsEnabled(true);
            var createButton = Assert.IsType<Button>(
                Assert.Single(form.Controls.Find("createQrButton", true)));
            createButton.PerformClick();

            Assert.Equal(IPAddress.Parse("192.168.1.50"), qrCodeService.Address);
        });
    }

    [Fact]
    public void LanAddressSelection_WithoutCandidates_ShowsDisabledGuidance()
    {
        RunInSta(() =>
        {
            using var form = new Form1();

            form.DisplayLanAddresses([], null);
            form.SetLanAddressSelectionEnabled(false);

            var comboBox = Assert.IsType<ComboBox>(
                Assert.Single(form.Controls.Find("lanAddressComboBox", true)));
            Assert.False(comboBox.Enabled);
            Assert.Equal("利用可能なLAN IPv4アドレスがありません", comboBox.Text);
        });
    }

    private static CreateTransferSessionResult CreateSuccessfulSession()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\one.jpg");
        return new CreateTransferSessionResult(
            true,
            new TransferImageQR.Domain.Sessions.TransferSession(
                "token",
                draft.Images,
                new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void RejectedImages_AreRenderedWithFileNameAndReason()
    {
        RunInSta(() =>
        {
            using var form = new Form1();

            form.DisplayRejectedImages(
                [new RejectedImageViewModel("large.jpg", "ファイルサイズが10MBを超えています。")]);

            var rejectionList = Assert.IsType<ListBox>(Assert.Single(form.Controls.Find("rejectionListBox", true)));
            Assert.Equal("large.jpg: ファイルサイズが10MBを超えています。", Assert.Single(rejectionList.Items.Cast<string>()));
        });
    }

    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(16, 12);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    private static byte[] CreateTransparentPng()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.Erase(new SKColor(255, 0, 0, 128));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    private static void RunInSta(Action action)
    {
        Exception? capturedException = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                capturedException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (capturedException is not null)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }
    }

    private static NotifyIcon GetTrayIcon(Form1 form)
    {
        var field = typeof(Form1).GetField("_trayIcon", BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<NotifyIcon>(field?.GetValue(form));
    }

    private sealed class StubAddImagesToDraftUseCase(AddImagesToDraftResult result)
        : IAddImagesToDraftUseCase
    {
        public Task<AddImagesToDraftResult> ExecuteAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class StubEditDraftUseCase : IEditDraftUseCase
    {
        public DraftEditResult Remove(Guid imageId) => new(false, 0);

        public DraftEditResult Clear() => new(false, 0);
    }

    private sealed class StubTransferSessionUseCase(
        CreateTransferSessionResult? createResult = null) : ITransferSessionUseCase
    {
        public bool CreateCalled { get; private set; }
        public bool StartNewTransferCalled { get; private set; }

        public CreateTransferSessionResult Create()
        {
            CreateCalled = true;
            return createResult ?? new(false, null);
        }

        public TransferImageQR.Domain.Sessions.TransferSession? GetCurrent() => createResult?.Session;

        public TransferSessionStatus? GetCurrentStatus() => createResult?.Session is null
            ? null
            : new TransferSessionStatus(
                TransferSessionState.Active,
                createResult.Session.ExpiresAt);

        public void StartNewTransfer() => StartNewTransferCalled = true;
    }

    private sealed class StubTransferQrCodeService : ITransferQrCodeService
    {
        public IPAddress? Address { get; private set; }

        public TransferQrCode? Create(string sessionToken, IPAddress? address)
        {
            Address = address;
            return null;
        }
    }

    private sealed class StubLanAddressProvider(params LanAddressOption[] options)
        : ILanAddressProvider
    {
        public IReadOnlyList<LanAddressOption> GetIPv4Addresses() => options;
    }

    private sealed class StubBackgroundCustomizationUseCase : IBackgroundCustomizationUseCase
    {
        public (int Opacity, int Zoom, int X, int Y)? Appearance { get; private set; }
        public bool Cleared { get; private set; }

        public BackgroundCustomizationResult Load() => Current();

        public BackgroundCustomizationResult SelectImage(string filePath) => Current();

        public BackgroundCustomizationResult UpdateAppearance(
            int opacityPercent,
            int zoomPercent,
            int offsetX,
            int offsetY)
        {
            Appearance = (opacityPercent, zoomPercent, offsetX, offsetY);
            return new BackgroundCustomizationResult(
                new BackgroundSettings(null, opacityPercent, zoomPercent, offsetX, offsetY),
                null);
        }

        public BackgroundCustomizationResult Clear()
        {
            Cleared = true;
            return Current();
        }

        private static BackgroundCustomizationResult Current() =>
            new(BackgroundSettings.Default, null);
    }

    private sealed class StubTraySettingsUseCase(bool initialValue) : ITraySettingsUseCase
    {
        public bool? SavedValue { get; private set; }

        public TraySettingsResult Load() => new(new TraySettings(initialValue));

        public TraySettingsResult SetMinimizeToTray(bool enabled)
        {
            SavedValue = enabled;
            return new TraySettingsResult(new TraySettings(enabled));
        }
    }
}

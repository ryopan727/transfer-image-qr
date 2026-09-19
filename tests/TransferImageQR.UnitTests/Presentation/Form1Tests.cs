using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using SkiaSharp;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class Form1Tests
{
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
                new StubTransferSessionUseCase());
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
            Assert.Contains("Active", stateLabel.Text);
            Assert.True(newTransferButton.Visible);

            form.DisplayTransferSession(new TransferSessionViewModel(true, expiresAt));
            Assert.Contains("Expired", stateLabel.Text);

            form.DisplayDraftState();
            Assert.Equal("状態: Draft", stateLabel.Text);
            Assert.False(newTransferButton.Visible);
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
                sessionUseCase);
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
}

using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using SkiaSharp;
using TransferImageQR.Application.Drafts;
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
            var useCase = new StubAddImagesToDraftUseCase(new AddImagesToDraftResult([image], 1));
            using var form = new Form1();
            var presenter = new MainPresenter(form, useCase);
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
}

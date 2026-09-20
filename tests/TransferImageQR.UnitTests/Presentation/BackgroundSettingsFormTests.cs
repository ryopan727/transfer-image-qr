using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class BackgroundSettingsFormTests
{
    [Fact]
    public void DedicatedForm_DisplaysCurrentValuesAndForwardsAllCommands()
    {
        RunInSta(() =>
        {
            var presenter = new FakeBackgroundSettingsPresenter();
            using var form = new BackgroundSettingsForm(
                presenter,
                _ => @"C:\images\background.png");
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            form.DisplaySettings(new BackgroundViewModel(CreatePng(), 40, 125, 5, -10));

            Assert.Equal("背景設定", form.AccessibleName);
            var preview = Assert.IsType<PictureBox>(Assert.Single(form.Controls.Find("backgroundPreviewPictureBox", true)));
            var select = Assert.IsType<Button>(Assert.Single(form.Controls.Find("selectBackgroundButton", true)));
            var clear = Assert.IsType<Button>(Assert.Single(form.Controls.Find("clearBackgroundButton", true)));
            var opacity = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundOpacityInput", true)));
            var zoom = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundZoomInput", true)));
            var offsetX = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundOffsetXInput", true)));
            var offsetY = Assert.IsType<NumericUpDown>(Assert.Single(form.Controls.Find("backgroundOffsetYInput", true)));

            Assert.True(presenter.Attached);
            Assert.NotNull(preview.Image);
            Assert.Equal(40, opacity.Value);
            Assert.Equal(125, zoom.Value);
            Assert.Equal(5, offsetX.Value);
            Assert.Equal(-10, offsetY.Value);
            Assert.True(clear.Enabled);

            select.PerformClick();
            opacity.Value = 65;
            clear.PerformClick();

            Assert.Equal(@"C:\images\background.png", presenter.SelectedPath);
            Assert.Equal((65, 125, 5, -10), presenter.Appearance);
            Assert.True(presenter.Cleared);

            form.Close();
            Assert.True(presenter.Detached);
        });
    }

    private static byte[] CreatePng()
    {
        using var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.CornflowerBlue);
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
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

    private sealed class FakeBackgroundSettingsPresenter : IBackgroundSettingsPresenter
    {
        public bool Attached { get; private set; }
        public bool Detached { get; private set; }
        public string? SelectedPath { get; private set; }
        public (int Opacity, int Zoom, int X, int Y)? Appearance { get; private set; }
        public bool Cleared { get; private set; }

        public void AttachBackgroundSettingsView(IBackgroundSettingsView view) => Attached = true;

        public void DetachBackgroundSettingsView(IBackgroundSettingsView view) => Detached = true;

        public void SelectBackgroundImage(string filePath) => SelectedPath = filePath;

        public void UpdateBackgroundAppearance(int opacityPercent, int zoomPercent, int offsetX, int offsetY) =>
            Appearance = (opacityPercent, zoomPercent, offsetX, offsetY);

        public void ClearBackground() => Cleared = true;
    }
}

using System.Runtime.ExceptionServices;
using System.Reflection;
using System.Windows.Forms;
using TransferImageQR.Presentation;
using Xunit;

namespace TransferImageQR.UnitTests.Presentation;

public sealed class ApplicationFontTests
{
    [Fact]
    public void EmbeddedFont_LoadsNotoSansJpAndSupportsRegularAndBold()
    {
        var fontBytes = ApplicationFonts.ReadEmbeddedFontBytes(typeof(Form1).Assembly);

        using var manager = ApplicationFontManager.Create(fontBytes);
        using var regular = manager.CreateFont(9F, FontStyle.Regular);
        using var bold = manager.CreateFont(14F, FontStyle.Bold);

        Assert.NotNull(fontBytes);
        Assert.True(manager.IsEmbeddedFontLoaded);
        Assert.Equal("Noto Sans JP", manager.Family.Name);
        Assert.Equal("Noto Sans JP", regular.Name);
        Assert.Equal("Noto Sans JP", bold.Name);
        Assert.True(bold.Bold);
    }

    [Fact]
    public void InvalidFontData_FallsBackToSystemFontWithoutThrowing()
    {
        using var manager = ApplicationFontManager.Create([0, 1, 2, 3]);
        using var font = manager.CreateFont(9F, FontStyle.Regular);

        Assert.False(manager.IsEmbeddedFontLoaded);
        var systemFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        Assert.Equal(systemFont.FontFamily.Name, manager.Family.Name);
        Assert.Equal(manager.Family.Name, font.Name);
    }

    [Fact]
    public void DesktopForms_UseNotoSansJpWhilePreservingHeadingSizeAndStyle()
    {
        RunInSta(() =>
        {
            ApplicationFonts.Initialize(typeof(Form1).Assembly);
            using var mainForm = new Form1();
            using var settingsForm = new BackgroundSettingsForm(new FakeBackgroundSettingsPresenter());

            var heading = Assert.IsType<Label>(Assert.Single(mainForm.Controls.Find("headingLabel", true)));
            var menu = Assert.IsType<MenuStrip>(Assert.Single(mainForm.Controls.Find("mainMenuStrip", true)));
            var trayIconField = typeof(Form1).GetField(
                "_trayIcon",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var trayIcon = Assert.IsType<NotifyIcon>(trayIconField?.GetValue(mainForm));
            var opacity = Assert.IsType<NumericUpDown>(Assert.Single(
                settingsForm.Controls.Find("backgroundOpacityInput", true)));

            Assert.Equal("Noto Sans JP", mainForm.Font.Name);
            Assert.Equal("Noto Sans JP", heading.Font.Name);
            Assert.Equal(24F, heading.Font.Size);
            Assert.True(heading.Font.Bold);
            Assert.Equal("Noto Sans JP", menu.Font.Name);
            Assert.Equal("Noto Sans JP", trayIcon.ContextMenuStrip?.Font.Name);
            Assert.Equal("Noto Sans JP", settingsForm.Font.Name);
            Assert.Equal("Noto Sans JP", opacity.Font.Name);
        });
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
        public void AttachBackgroundSettingsView(IBackgroundSettingsView view)
        {
        }

        public void DetachBackgroundSettingsView(IBackgroundSettingsView view)
        {
        }

        public void SelectBackgroundImage(string filePath)
        {
        }

        public void UpdateBackgroundAppearance(
            int opacityPercent,
            int zoomPercent,
            int offsetX,
            int offsetY)
        {
        }

        public void ClearBackground()
        {
        }
    }
}

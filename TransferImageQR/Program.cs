using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Infrastructure.Images;
using TransferImageQR.Infrastructure.Files;
using TransferImageQR.Infrastructure.Security;
using TransferImageQR.Infrastructure.Http;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Presentation;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Infrastructure.Networking;
using TransferImageQR.Infrastructure.QrCodes;
using TransferImageQR.Application.Backgrounds;
using TransferImageQR.Infrastructure.Settings;
using TransferImageQR.Application.Tray;
using TransferImageQR.Application.AutoStart;
using TransferImageQR.Infrastructure.Windows;

namespace TransferImageQR
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var draft = new TransferDraft();
            var thumbnailProvider = new SkiaImageThumbnailProvider();
            var fileMetadataProvider = new PhysicalFileMetadataProvider();
            var addImagesToDraft = new AddImagesToDraftUseCase(
                draft,
                thumbnailProvider,
                fileMetadataProvider);
            var editDraft = new EditDraftUseCase(draft);
            var transferSession = new TransferSessionUseCase(
                draft,
                new CryptographicSessionTokenGenerator(),
                TimeProvider.System);
            var httpServer = new LanImageHttpServer(transferSession);
            var serverStartFailed = false;
            try
            {
                httpServer.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                serverStartFailed = true;
            }
            var lanAddressProvider = new SystemLanAddressProvider();
            var transferUrlProvider = new TransferUrlProvider(httpServer);
            var transferQrCode = new TransferQrCodeService(
                transferUrlProvider,
                new QrCoderPngGenerator());
            var backgroundSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TransferImageQR",
                "background-settings.json");
            var backgroundCustomization = new BackgroundCustomizationUseCase(
                new JsonBackgroundSettingsStore(backgroundSettingsPath),
                new SkiaBackgroundImageLoader());
            var traySettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TransferImageQR",
                "tray-settings.json");
            var traySettings = new TraySettingsUseCase(
                new JsonTraySettingsStore(traySettingsPath));
            var autoStartSettings = new AutoStartSettingsUseCase(
                new RegistryAutoStartRegistration(),
                Environment.ProcessPath);

            try
            {
                using var mainForm = new Form1();
                var presenter = new MainPresenter(
                    mainForm,
                    addImagesToDraft,
                    editDraft,
                    transferSession,
                    transferQrCode,
                    lanAddressProvider,
                    backgroundCustomization,
                    traySettings,
                    autoStartSettings,
                    httpServer,
                    serverStartFailed);
                mainForm.AttachPresenter(presenter);
                presenter.Initialize();
                presenter.LoadBackground();
                presenter.LoadTraySettings();
                presenter.LoadAutoStartSettings();
                System.Windows.Forms.Application.Run(mainForm);
            }
            finally
            {
                httpServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}

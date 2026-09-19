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
            httpServer.StartAsync().GetAwaiter().GetResult();
            var transferUrlProvider = new TransferUrlProvider(
                httpServer,
                new SystemLanAddressProvider());
            var transferQrCode = new TransferQrCodeService(
                transferUrlProvider,
                new QrCoderPngGenerator());

            try
            {
                using var mainForm = new Form1();
                var presenter = new MainPresenter(
                    mainForm,
                    addImagesToDraft,
                    editDraft,
                    transferSession,
                    transferQrCode);
                mainForm.AttachPresenter(presenter);
                System.Windows.Forms.Application.Run(mainForm);
            }
            finally
            {
                httpServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}

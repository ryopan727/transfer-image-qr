using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Infrastructure.Images;
using TransferImageQR.Infrastructure.Files;
using TransferImageQR.Presentation;

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
            using var mainForm = new Form1();
            var presenter = new MainPresenter(mainForm, addImagesToDraft, editDraft);
            mainForm.AttachPresenter(presenter);

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}

using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Application.Drafts;

public sealed class AddImagesToDraftUseCase(
    TransferDraft draft,
    IImageThumbnailProvider thumbnailProvider,
    IFileMetadataProvider fileMetadataProvider) : IAddImagesToDraftUseCase
{
    private const long MaximumFileSizeBytes = 10L * 1024 * 1024;
    private const int ThumbnailMaximumWidth = 160;
    private const int ThumbnailMaximumHeight = 120;

    public async Task<AddImagesToDraftResult> ExecuteAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var addedImages = new List<DraftImageListItem>();
        var rejectedImages = new List<RejectedDraftImage>();

        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(filePath) ||
                !ImageFileFormatDetector.TryDetect(filePath, out var expectedFormat))
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.UnsupportedFormat));
                continue;
            }

            if (draft.Images.Count >= TransferDraft.MaximumImageCount)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.DraftLimitReached));
                continue;
            }

            var metadata = fileMetadataProvider.Get(filePath);
            if (!metadata.Success)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.UnreadableImage));
                continue;
            }

            if (metadata.Length > MaximumFileSizeBytes)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.FileTooLarge));
                continue;
            }

            var thumbnail = await thumbnailProvider.CreateAsync(
                filePath,
                ThumbnailMaximumWidth,
                ThumbnailMaximumHeight,
                cancellationToken);

            if (!thumbnail.Success || thumbnail.PngBytes is null)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.UnreadableImage));
                continue;
            }

            if (thumbnail.DetectedFormat != expectedFormat)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.FileFormatMismatch));
                continue;
            }

            if (!draft.TryAdd(filePath, out var draftImage) || draftImage is null)
            {
                rejectedImages.Add(new RejectedDraftImage(
                    filePath,
                    fileName,
                    DraftImageRejectionReason.DraftLimitReached));
                continue;
            }

            addedImages.Add(new DraftImageListItem(
                draftImage.Id,
                draftImage.FilePath,
                draftImage.FileName,
                thumbnail.PngBytes));
        }

        return new AddImagesToDraftResult(addedImages, draft.Images.Count, rejectedImages);
    }
}

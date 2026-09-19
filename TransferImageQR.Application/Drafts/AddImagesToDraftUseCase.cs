using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Application.Drafts;

public sealed class AddImagesToDraftUseCase(
    TransferDraft draft,
    IImageThumbnailProvider thumbnailProvider) : IAddImagesToDraftUseCase
{
    private const int ThumbnailMaximumWidth = 160;
    private const int ThumbnailMaximumHeight = 120;

    public async Task<AddImagesToDraftResult> ExecuteAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var addedImages = new List<DraftImageListItem>();

        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(filePath) ||
                !ImageFileFormatDetector.TryDetect(filePath, out _))
            {
                continue;
            }

            var thumbnail = await thumbnailProvider.CreateAsync(
                filePath,
                ThumbnailMaximumWidth,
                ThumbnailMaximumHeight,
                cancellationToken);

            if (!thumbnail.Success || thumbnail.PngBytes is null)
            {
                continue;
            }

            var draftImage = draft.Add(filePath);
            addedImages.Add(new DraftImageListItem(
                draftImage.Id,
                draftImage.FilePath,
                draftImage.FileName,
                thumbnail.PngBytes));
        }

        return new AddImagesToDraftResult(addedImages, draft.Images.Count);
    }
}

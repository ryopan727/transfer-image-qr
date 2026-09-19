using TransferImageQR.Application.Drafts;

namespace TransferImageQR.Presentation;

public sealed class MainPresenter(
    IMainView view,
    IAddImagesToDraftUseCase addImagesToDraftUseCase)
{
    private bool _isAdding;

    public async Task AddDroppedFilesAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        if (_isAdding || filePaths.Count == 0)
        {
            return;
        }

        _isAdding = true;
        view.SetDropEnabled(false);

        try
        {
            var result = await addImagesToDraftUseCase.ExecuteAsync(filePaths, cancellationToken);
            var viewModels = result.AddedImages
                .Select(image => new DraftImageViewModel(
                    image.Id,
                    image.FilePath,
                    image.FileName,
                    image.ThumbnailPng))
                .ToArray();

            if (viewModels.Length > 0)
            {
                view.AppendDraftImages(viewModels);
            }

            view.SetDraftCount(result.TotalCount);
        }
        finally
        {
            _isAdding = false;
            view.SetDropEnabled(true);
        }
    }
}

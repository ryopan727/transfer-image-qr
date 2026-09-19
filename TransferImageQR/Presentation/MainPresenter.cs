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
            view.DisplayRejectedImages(result.RejectedImages
                .Select(rejection => new RejectedImageViewModel(
                    rejection.FileName,
                    ToUserMessage(rejection.Reason)))
                .ToArray());
        }
        finally
        {
            _isAdding = false;
            view.SetDropEnabled(true);
        }
    }

    private static string ToUserMessage(DraftImageRejectionReason reason) =>
        reason switch
        {
            DraftImageRejectionReason.UnsupportedFormat => "対応していない形式です。",
            DraftImageRejectionReason.FileTooLarge => "ファイルサイズが10MBを超えています。",
            DraftImageRejectionReason.DraftLimitReached => "Draftは最大20枚です。",
            DraftImageRejectionReason.UnreadableImage => "画像を読み込めません。",
            DraftImageRejectionReason.FileFormatMismatch => "拡張子と画像形式が一致しません。",
            _ => "画像を追加できません。",
        };
}

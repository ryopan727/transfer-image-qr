using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Application.Transfers;

namespace TransferImageQR.Presentation;

public sealed class MainPresenter(
    IMainView view,
    IAddImagesToDraftUseCase addImagesToDraftUseCase,
    IEditDraftUseCase editDraftUseCase,
    ITransferSessionUseCase transferSessionUseCase,
    ITransferQrCodeService transferQrCodeService)
{
    private bool _isAdding;
    private bool _isEditingEnabled = true;
    private int _draftCount;
    private TransferQrCode? _transferQrCode;

    public async Task AddDroppedFilesAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        if (!_isEditingEnabled || _isAdding || filePaths.Count == 0)
        {
            return;
        }

        _isAdding = true;
        view.SetDropEnabled(false);
        view.SetDraftActionsEnabled(false);

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

            UpdateDraftState(result.TotalCount);
            view.DisplayRejectedImages(result.RejectedImages
                .Select(rejection => new RejectedImageViewModel(
                    rejection.FileName,
                    ToUserMessage(rejection.Reason)))
                .ToArray());
        }
        finally
        {
            _isAdding = false;
            view.SetDropEnabled(_isEditingEnabled);
            view.SetDraftActionsEnabled(_isEditingEnabled && _draftCount > 0);
        }
    }

    public void RemoveDraftImage(Guid imageId)
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = editDraftUseCase.Remove(imageId);
        if (result.Changed)
        {
            view.RemoveDraftImage(imageId);
        }

        UpdateDraftState(result.TotalCount);
    }

    public void ClearDraft()
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = editDraftUseCase.Clear();
        if (result.Changed)
        {
            view.ClearDraftImages();
        }

        UpdateDraftState(result.TotalCount);
    }

    public void SetDraftEditingEnabled(bool enabled)
    {
        _isEditingEnabled = enabled;
        view.SetDropEnabled(enabled && !_isAdding);
        view.SetDraftActionsEnabled(enabled && !_isAdding && _draftCount > 0);
        view.SetDraftEditingEnabled(enabled);
    }

    public void CreateTransferSession()
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = transferSessionUseCase.Create();
        if (!result.Success)
        {
            return;
        }

        _transferQrCode = result.Session is null
            ? null
            : transferQrCodeService.Create(result.Session.Token);
        SetDraftEditingEnabled(false);
        RefreshTransferSessionState();
    }

    public void RefreshTransferSessionState()
    {
        var status = transferSessionUseCase.GetCurrentStatus();
        if (status is null)
        {
            view.DisplayDraftState();
            return;
        }

        view.DisplayTransferSession(new TransferSessionViewModel(
            status.State == TransferSessionState.Expired,
            status.ExpiresAt,
            _transferQrCode?.Url.AbsoluteUri,
            _transferQrCode?.PngBytes));
    }

    public void StartNewTransfer()
    {
        if (_isAdding)
        {
            return;
        }

        transferSessionUseCase.StartNewTransfer();
        _transferQrCode = null;
        view.ClearDraftImages();
        view.DisplayRejectedImages([]);
        UpdateDraftState(0);
        SetDraftEditingEnabled(true);
        view.DisplayDraftState();
    }

    private void UpdateDraftState(int count)
    {
        _draftCount = count;
        view.SetDraftCount(count);
        view.SetDraftActionsEnabled(_isEditingEnabled && count > 0);
    }

    private static string ToUserMessage(DraftImageRejectionReason reason) =>
        reason switch
        {
            DraftImageRejectionReason.UnsupportedFormat => "対応していない形式です。",
            DraftImageRejectionReason.FileTooLarge => "ファイルサイズが10MBを超えています。",
            DraftImageRejectionReason.DraftLimitReached => "Draftは最大20枚です。",
            DraftImageRejectionReason.UnreadableImage => "画像を読み込めません。",
            DraftImageRejectionReason.FileFormatMismatch => "拡張子と画像形式が一致しません。",
            DraftImageRejectionReason.DraftNotEditable => "転送中はDraftを変更できません。",
            _ => "画像を追加できません。",
        };
}

namespace TransferImageQR.Presentation;

public interface IMainView
{
    void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images);

    void SetDraftCount(int count);

    void SetDropEnabled(bool enabled);

    void DisplayRejectedImages(IReadOnlyCollection<RejectedImageViewModel> images);
}

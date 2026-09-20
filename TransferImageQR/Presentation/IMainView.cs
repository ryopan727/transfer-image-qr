namespace TransferImageQR.Presentation;

public interface IMainView
{
    void DisplayLanAddresses(
        IReadOnlyCollection<LanAddressViewModel> addresses,
        string? selectedAddress);

    void SetLanAddressSelectionEnabled(bool enabled);

    void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images);

    void SetDraftCount(int count);

    void SetDropEnabled(bool enabled);

    void DisplayRejectedImages(IReadOnlyCollection<RejectedImageViewModel> images);

    void RemoveDraftImage(Guid imageId);

    void ClearDraftImages();

    void SetDraftActionsEnabled(bool enabled);

    void SetDraftEditingEnabled(bool enabled);

    void DisplayTransferSession(TransferSessionViewModel session);

    void DisplayDraftState();

    void ApplyBackground(BackgroundViewModel background);

    void DisplayBackgroundError(string? message);
}

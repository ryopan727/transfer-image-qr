namespace TransferImageQR.Application.Drafts;

public sealed record AddImagesToDraftResult(
    IReadOnlyList<DraftImageListItem> AddedImages,
    int TotalCount,
    IReadOnlyList<RejectedDraftImage> RejectedImages);

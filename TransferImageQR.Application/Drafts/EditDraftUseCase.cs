using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Application.Drafts;

public sealed class EditDraftUseCase(TransferDraft draft) : IEditDraftUseCase
{
    public DraftEditResult Remove(Guid imageId) =>
        new(draft.Remove(imageId), draft.Images.Count);

    public DraftEditResult Clear()
    {
        var changed = draft.Images.Count > 0;
        draft.Clear();
        return new DraftEditResult(changed, draft.Images.Count);
    }
}

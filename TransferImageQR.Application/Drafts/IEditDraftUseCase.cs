namespace TransferImageQR.Application.Drafts;

public interface IEditDraftUseCase
{
    DraftEditResult Remove(Guid imageId);

    DraftEditResult Clear();
}

namespace TransferImageQR.Application.Drafts;

public sealed record RejectedDraftImage(
    string FilePath,
    string FileName,
    DraftImageRejectionReason Reason);

public enum DraftImageRejectionReason
{
    UnsupportedFormat,
    FileTooLarge,
    DraftLimitReached,
    UnreadableImage,
    FileFormatMismatch,
    DraftNotEditable,
}

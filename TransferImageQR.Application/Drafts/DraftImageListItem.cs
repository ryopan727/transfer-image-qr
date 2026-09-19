namespace TransferImageQR.Application.Drafts;

public sealed record DraftImageListItem(
    Guid Id,
    string FilePath,
    string FileName,
    byte[] ThumbnailPng);

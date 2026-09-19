namespace TransferImageQR.Presentation;

public sealed record DraftImageViewModel(
    Guid Id,
    string FilePath,
    string FileName,
    byte[] ThumbnailPng);

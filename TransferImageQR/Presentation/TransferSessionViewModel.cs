namespace TransferImageQR.Presentation;

public sealed record TransferSessionViewModel(
    bool IsExpired,
    DateTimeOffset ExpiresAt,
    string? TransferUrl = null,
    byte[]? QrCodePng = null);

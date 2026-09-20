namespace TransferImageQR.Presentation;

public sealed record NetworkDiagnosticsViewModel(
    bool ServerRunning,
    bool ServerStartFailed,
    string? SelectedAddress,
    int Port);

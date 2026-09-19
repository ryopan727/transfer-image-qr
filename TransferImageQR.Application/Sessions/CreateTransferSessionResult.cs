using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public sealed record CreateTransferSessionResult(bool Success, TransferSession? Session);

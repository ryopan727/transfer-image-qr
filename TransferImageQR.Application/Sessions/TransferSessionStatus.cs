using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public sealed record TransferSessionStatus(
    TransferSessionState State,
    DateTimeOffset ExpiresAt);

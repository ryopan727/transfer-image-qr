using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public sealed record TransferSessionAccess
{
    private TransferSessionAccess(
        TransferSessionAccessStatus status,
        TransferSession? session)
    {
        Status = status;
        Session = session;
    }

    public TransferSessionAccessStatus Status { get; }

    public TransferSession? Session { get; }

    public static TransferSessionAccess NotFound { get; } =
        new(TransferSessionAccessStatus.NotFound, null);

    public static TransferSessionAccess Expired { get; } =
        new(TransferSessionAccessStatus.Expired, null);

    public static TransferSessionAccess Active(TransferSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new TransferSessionAccess(TransferSessionAccessStatus.Active, session);
    }
}

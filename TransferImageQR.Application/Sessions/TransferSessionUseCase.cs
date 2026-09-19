using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public sealed class TransferSessionUseCase(
    TransferDraft draft,
    ISessionTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : ITransferSessionUseCase
{
    private readonly object _sync = new();
    private TransferSession? _currentSession;

    public CreateTransferSessionResult Create()
    {
        lock (_sync)
        {
            if (!draft.IsEditable || draft.Images.Count == 0)
            {
                return new CreateTransferSessionResult(false, null);
            }

            var token = tokenGenerator.Generate();
            var session = new TransferSession(token, draft.Images, timeProvider.GetUtcNow());
            if (!draft.Confirm())
            {
                return new CreateTransferSessionResult(false, null);
            }

            _currentSession = session;
            return new CreateTransferSessionResult(true, session);
        }
    }

    public TransferSession? GetCurrent()
    {
        lock (_sync)
        {
            return _currentSession;
        }
    }

    public TransferSessionStatus? GetCurrentStatus()
    {
        lock (_sync)
        {
            return _currentSession is null
                ? null
                : new TransferSessionStatus(
                    _currentSession.GetState(timeProvider.GetUtcNow()),
                    _currentSession.ExpiresAt);
        }
    }

    public void StartNewTransfer()
    {
        lock (_sync)
        {
            _currentSession = null;
            draft.Reset();
        }
    }
}

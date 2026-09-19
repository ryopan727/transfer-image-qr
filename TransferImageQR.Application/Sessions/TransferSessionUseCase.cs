using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using System.Security.Cryptography;
using System.Text;

namespace TransferImageQR.Application.Sessions;

public sealed class TransferSessionUseCase(
    TransferDraft draft,
    ISessionTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : ITransferSessionUseCase, IActiveTransferSessionProvider
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

    public TransferSession? GetActive(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        lock (_sync)
        {
            if (_currentSession is null ||
                _currentSession.GetState(timeProvider.GetUtcNow()) != TransferSessionState.Active ||
                !TokensMatch(_currentSession.Token, token))
            {
                return null;
            }

            return _currentSession;
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

    private static bool TokensMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

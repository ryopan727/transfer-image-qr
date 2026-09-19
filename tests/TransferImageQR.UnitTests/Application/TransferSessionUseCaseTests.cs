using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class TransferSessionUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithDraftImages_ConfirmsDraftAndCreatesActiveSession()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\first.jpg");
        draft.Add(@"C:\images\second.png");
        var sut = new TransferSessionUseCase(
            draft,
            new StubTokenGenerator("generated-token"),
            new StubTimeProvider(Now));

        var result = sut.Create();

        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.Equal("generated-token", result.Session.Token);
        Assert.Equal(2, result.Session.Images.Count);
        Assert.Equal(TransferSessionState.Active, result.Session.GetState(Now));
        Assert.False(draft.IsEditable);
    }

    [Fact]
    public void Create_WithEmptyDraft_FailsWithoutGeneratingToken()
    {
        var draft = new TransferDraft();
        var tokenGenerator = new StubTokenGenerator("unused");
        var sut = new TransferSessionUseCase(draft, tokenGenerator, new StubTimeProvider(Now));

        var result = sut.Create();

        Assert.False(result.Success);
        Assert.Null(result.Session);
        Assert.False(tokenGenerator.WasCalled);
        Assert.True(draft.IsEditable);
    }

    [Fact]
    public void StartNewTransfer_AfterActiveSession_ClearsSessionAndResetsDraft()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\first.jpg");
        var sut = new TransferSessionUseCase(
            draft,
            new StubTokenGenerator("generated-token"),
            new StubTimeProvider(Now));
        sut.Create();

        sut.StartNewTransfer();

        Assert.Null(sut.GetCurrent());
        Assert.True(draft.IsEditable);
        Assert.Empty(draft.Images);
    }

    [Fact]
    public void GetCurrentStatus_UsesInjectedClockAtExpirationBoundary()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\first.jpg");
        var timeProvider = new StubTimeProvider(Now);
        var sut = new TransferSessionUseCase(
            draft,
            new StubTokenGenerator("generated-token"),
            timeProvider);
        var session = Assert.IsType<TransferSession>(sut.Create().Session);

        timeProvider.UtcNow = session.ExpiresAt.AddTicks(-1);
        Assert.Equal(TransferSessionState.Active, sut.GetCurrentStatus()?.State);

        timeProvider.UtcNow = session.ExpiresAt;
        Assert.Equal(TransferSessionState.Expired, sut.GetCurrentStatus()?.State);
    }

    private sealed class StubTokenGenerator(string token) : ISessionTokenGenerator
    {
        public bool WasCalled { get; private set; }

        public string Generate()
        {
            WasCalled = true;
            return token;
        }
    }

    private sealed class StubTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

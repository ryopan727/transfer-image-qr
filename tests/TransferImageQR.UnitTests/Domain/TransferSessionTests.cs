using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using Xunit;

namespace TransferImageQR.UnitTests.Domain;

public sealed class TransferSessionTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CopiesDraftImagesAndSetsFiveMinuteExpiration()
    {
        var draft = new TransferDraft();
        var first = draft.Add(@"C:\images\first.jpg");
        var second = draft.Add(@"C:\images\second.png");

        var session = new TransferSession("secure-token", draft.Images, CreatedAt);
        draft.Reset();

        Assert.Equal(CreatedAt, session.CreatedAt);
        Assert.Equal(CreatedAt.AddMinutes(5), session.ExpiresAt);
        Assert.Collection(
            session.Images,
            image => Assert.Same(first, image),
            image => Assert.Same(second, image));
    }

    [Fact]
    public void GetState_ImmediatelyBeforeExpiration_ReturnsActive()
    {
        var session = CreateSession();

        var state = session.GetState(session.ExpiresAt.AddTicks(-1));

        Assert.Equal(TransferSessionState.Active, state);
    }

    [Fact]
    public void GetState_AtExpiration_ReturnsExpired()
    {
        var session = CreateSession();

        var state = session.GetState(session.ExpiresAt);

        Assert.Equal(TransferSessionState.Expired, state);
    }

    private static TransferSession CreateSession()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\image.webp");
        return new TransferSession("secure-token", draft.Images, CreatedAt);
    }
}

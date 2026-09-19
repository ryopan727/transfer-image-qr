using System.Collections.ObjectModel;
using TransferImageQR.Domain.Drafts;

namespace TransferImageQR.Domain.Sessions;

public sealed class TransferSession
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ReadOnlyCollection<DraftImage> _images;

    public TransferSession(
        string token,
        IReadOnlyCollection<DraftImage> images,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(images);

        if (images.Count == 0)
        {
            throw new ArgumentException("A transfer session requires at least one image.", nameof(images));
        }

        Token = token;
        _images = Array.AsReadOnly(images.ToArray());
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(Lifetime);
    }

    public string Token { get; }

    public IReadOnlyList<DraftImage> Images => _images;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public TransferSessionState GetState(DateTimeOffset currentTime) =>
        currentTime < ExpiresAt
            ? TransferSessionState.Active
            : TransferSessionState.Expired;
}

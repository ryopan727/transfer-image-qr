namespace TransferImageQR.Domain.Drafts;

public sealed class TransferDraft
{
    public const int MaximumImageCount = 20;

    private readonly List<DraftImage> _images = [];

    public IReadOnlyList<DraftImage> Images => _images;

    public DraftImage Add(string filePath)
    {
        if (!TryAdd(filePath, out var image))
        {
            throw new InvalidOperationException($"A draft cannot contain more than {MaximumImageCount} images.");
        }

        return image!;
    }

    public bool TryAdd(string filePath, out DraftImage? image)
    {
        if (_images.Count >= MaximumImageCount)
        {
            image = null;
            return false;
        }

        image = new DraftImage(filePath);
        _images.Add(image);
        return true;
    }

    public bool Remove(Guid imageId)
    {
        var image = _images.Find(candidate => candidate.Id == imageId);
        return image is not null && _images.Remove(image);
    }

    public void Clear() => _images.Clear();
}

namespace TransferImageQR.Domain.Drafts;

public sealed class TransferDraft
{
    public const int MaximumImageCount = 20;

    private readonly List<DraftImage> _images = [];

    public IReadOnlyList<DraftImage> Images => _images;

    public bool IsEditable { get; private set; } = true;

    public DraftImage Add(string filePath)
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException("A confirmed draft cannot be changed.");
        }

        if (!TryAdd(filePath, out var image))
        {
            throw new InvalidOperationException($"A draft cannot contain more than {MaximumImageCount} images.");
        }

        return image!;
    }

    public bool TryAdd(string filePath, out DraftImage? image)
    {
        if (!IsEditable || _images.Count >= MaximumImageCount)
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
        if (!IsEditable)
        {
            return false;
        }

        var image = _images.Find(candidate => candidate.Id == imageId);
        return image is not null && _images.Remove(image);
    }

    public bool Clear()
    {
        if (!IsEditable)
        {
            return false;
        }

        var changed = _images.Count > 0;
        _images.Clear();
        return changed;
    }

    public bool Confirm()
    {
        if (!IsEditable || _images.Count == 0)
        {
            return false;
        }

        IsEditable = false;
        return true;
    }

    public void Reset()
    {
        _images.Clear();
        IsEditable = true;
    }
}

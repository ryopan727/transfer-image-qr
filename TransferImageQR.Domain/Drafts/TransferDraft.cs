namespace TransferImageQR.Domain.Drafts;

public sealed class TransferDraft
{
    private readonly List<DraftImage> _images = [];

    public IReadOnlyList<DraftImage> Images => _images;

    public DraftImage Add(string filePath)
    {
        var image = new DraftImage(filePath);
        _images.Add(image);
        return image;
    }
}

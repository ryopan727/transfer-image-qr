namespace TransferImageQR.Domain.Drafts;

public sealed class DraftImage
{
    internal DraftImage(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Id = Guid.NewGuid();
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }

    public Guid Id { get; }

    public string FilePath { get; }

    public string FileName { get; }
}

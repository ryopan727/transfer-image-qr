namespace TransferImageQR.Application.Drafts;

public interface IFileMetadataProvider
{
    FileMetadataResult Get(string filePath);
}

public sealed record FileMetadataResult
{
    private FileMetadataResult(bool success, long length)
    {
        Success = success;
        Length = length;
    }

    public static FileMetadataResult Failed { get; } = new(false, 0);

    public bool Success { get; }

    public long Length { get; }

    public static FileMetadataResult Succeeded(long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return new FileMetadataResult(true, length);
    }
}

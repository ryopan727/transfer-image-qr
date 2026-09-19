using TransferImageQR.Application.Drafts;

namespace TransferImageQR.Infrastructure.Files;

public sealed class PhysicalFileMetadataProvider : IFileMetadataProvider
{
    public FileMetadataResult Get(string filePath)
    {
        try
        {
            var file = new FileInfo(filePath);
            return file.Exists
                ? FileMetadataResult.Succeeded(file.Length)
                : FileMetadataResult.Failed;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return FileMetadataResult.Failed;
        }
    }
}

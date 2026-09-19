namespace TransferImageQR.Application.Drafts;

public interface IAddImagesToDraftUseCase
{
    Task<AddImagesToDraftResult> ExecuteAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken);
}

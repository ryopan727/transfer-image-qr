namespace TransferImageQR.Application.Transfers;

public interface IHttpServerEndpoint
{
    bool IsRunning { get; }

    int Port { get; }
}

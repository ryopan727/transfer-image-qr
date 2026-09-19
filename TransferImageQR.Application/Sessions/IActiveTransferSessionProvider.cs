using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public interface IActiveTransferSessionProvider
{
    TransferSession? GetActive(string token);
}

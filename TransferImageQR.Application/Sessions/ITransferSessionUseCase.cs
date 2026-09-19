using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Application.Sessions;

public interface ITransferSessionUseCase
{
    CreateTransferSessionResult Create();

    TransferSession? GetCurrent();

    TransferSessionStatus? GetCurrentStatus();

    void StartNewTransfer();
}

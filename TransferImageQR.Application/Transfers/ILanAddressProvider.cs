namespace TransferImageQR.Application.Transfers;

public interface ILanAddressProvider
{
    IReadOnlyList<LanAddressOption> GetIPv4Addresses();
}

using System.Net;

namespace TransferImageQR.Application.Transfers;

public sealed record LanAddressOption(IPAddress Address, string InterfaceName);


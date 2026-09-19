using System.Net;
using System.Net.NetworkInformation;

namespace TransferImageQR.Infrastructure.Networking;

public sealed record LanAddressCandidate(
    IPAddress Address,
    NetworkInterfaceType InterfaceType,
    OperationalStatus OperationalStatus,
    int InterfaceIndex);

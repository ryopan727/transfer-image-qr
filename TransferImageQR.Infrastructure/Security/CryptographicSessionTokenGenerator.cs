using System.Security.Cryptography;
using TransferImageQR.Application.Sessions;

namespace TransferImageQR.Infrastructure.Security;

public sealed class CryptographicSessionTokenGenerator : ISessionTokenGenerator
{
    private const int TokenByteLength = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

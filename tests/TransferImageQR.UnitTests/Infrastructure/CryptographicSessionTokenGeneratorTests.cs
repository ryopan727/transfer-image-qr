using System.Text.RegularExpressions;
using TransferImageQR.Infrastructure.Security;
using Xunit;

namespace TransferImageQR.UnitTests.Infrastructure;

public sealed partial class CryptographicSessionTokenGeneratorTests
{
    [Fact]
    public void Generate_ReturnsUnique256BitBase64UrlTokens()
    {
        var sut = new CryptographicSessionTokenGenerator();

        var tokens = Enumerable.Range(0, 100).Select(_ => sut.Generate()).ToArray();

        Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());
        Assert.All(tokens, token =>
        {
            Assert.Equal(43, token.Length);
            Assert.Matches(Base64UrlPattern(), token);
        });
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex Base64UrlPattern();
}

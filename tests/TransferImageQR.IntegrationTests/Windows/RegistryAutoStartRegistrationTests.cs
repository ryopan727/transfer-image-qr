using Microsoft.Win32;
using System.Runtime.Versioning;
using TransferImageQR.Infrastructure.Windows;
using Xunit;

namespace TransferImageQR.IntegrationTests.Windows;

[SupportedOSPlatform("windows")]
public sealed class RegistryAutoStartRegistrationTests : IDisposable
{
    private readonly string _keyPath = $@"Software\TransferImageQR\Tests\{Guid.NewGuid():N}";
    private const string ValueName = "TransferImageQR-Test";

    [Fact]
    public void EnableAndDisable_RoundTripsQuotedExecutableWithoutChangingOtherValues()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true))
        {
            key.SetValue("OtherApplication", "keep-me");
        }

        var sut = new RegistryAutoStartRegistration(_keyPath, ValueName);
        var executablePath = @"C:\Program Files\TransferImageQR\TransferImageQR.exe";

        sut.Enable(executablePath);

        Assert.True(sut.IsEnabled(executablePath));
        using (var key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: false))
        {
            Assert.Equal($"\"{executablePath}\"", key?.GetValue(ValueName));
        }

        sut.Disable();

        Assert.False(sut.IsEnabled(executablePath));
        using var remainingKey = Registry.CurrentUser.OpenSubKey(_keyPath, writable: false);
        Assert.Equal("keep-me", remainingKey?.GetValue("OtherApplication"));
    }

    [Fact]
    public void IsEnabled_ReturnsFalseForDifferentExecutableOrArguments()
    {
        using var key = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true);
        key.SetValue(ValueName, @"""C:\Apps\Old\TransferImageQR.exe"" --hidden");
        var sut = new RegistryAutoStartRegistration(_keyPath, ValueName);

        Assert.False(sut.IsEnabled(@"C:\Apps\TransferImageQR.exe"));
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
}

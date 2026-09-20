using Microsoft.Win32;
using System.Runtime.Versioning;
using TransferImageQR.Application.AutoStart;

namespace TransferImageQR.Infrastructure.Windows;

[SupportedOSPlatform("windows")]
public sealed class RegistryAutoStartRegistration(
    string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
    string valueName = "TransferImageQR") : IAutoStartRegistration
{
    public bool IsEnabled(string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath, writable: false);
        return key?.GetValue(valueName) is string command &&
            string.Equals(command, BuildCommand(executablePath), StringComparison.OrdinalIgnoreCase);
    }

    public void Enable(string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(registryKeyPath, writable: true);
        key.SetValue(valueName, BuildCommand(executablePath), RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    public static string BuildCommand(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return $"\"{Path.GetFullPath(executablePath)}\"";
    }
}

using TransferImageQR.Application.AutoStart;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class AutoStartSettingsUseCaseTests
{
    [Fact]
    public void Load_ReflectsRegistrationForCurrentExecutable()
    {
        var registration = new StubRegistration { Enabled = true };
        var sut = new AutoStartSettingsUseCase(registration, @"C:\Program Files\TransferImageQR.exe");

        var result = sut.Load();

        Assert.True(result.Enabled);
        Assert.Equal(@"C:\Program Files\TransferImageQR.exe", registration.CheckedPath);
        Assert.Equal(AutoStartSettingsError.None, result.Error);
    }

    [Fact]
    public void SetEnabled_RegistersAndRemovesCurrentExecutable()
    {
        var registration = new StubRegistration();
        var sut = new AutoStartSettingsUseCase(registration, @"C:\Apps\TransferImageQR.exe");

        Assert.True(sut.SetEnabled(true).Enabled);
        Assert.Equal(@"C:\Apps\TransferImageQR.exe", registration.EnabledPath);
        Assert.False(sut.SetEnabled(false).Enabled);
        Assert.True(registration.Disabled);
    }

    [Fact]
    public void RegistrationFailure_PreservesLastKnownStateAndReturnsFriendlyError()
    {
        var registration = new StubRegistration { Enabled = true };
        var sut = new AutoStartSettingsUseCase(registration, @"C:\Apps\TransferImageQR.exe");
        Assert.True(sut.Load().Enabled);
        registration.Exception = new UnauthorizedAccessException();

        var result = sut.SetEnabled(false);

        Assert.True(result.Enabled);
        Assert.Equal(AutoStartSettingsError.RegistrationUnavailable, result.Error);
    }

    [Fact]
    public void MissingExecutablePath_ReturnsUnavailableWithoutAccessingRegistration()
    {
        var registration = new StubRegistration();
        var sut = new AutoStartSettingsUseCase(registration, null);

        var result = sut.Load();

        Assert.False(result.Enabled);
        Assert.Equal(AutoStartSettingsError.RegistrationUnavailable, result.Error);
        Assert.Null(registration.CheckedPath);
    }

    private sealed class StubRegistration : IAutoStartRegistration
    {
        public bool Enabled { get; set; }
        public Exception? Exception { get; set; }
        public string? CheckedPath { get; private set; }
        public string? EnabledPath { get; private set; }
        public bool Disabled { get; private set; }

        public bool IsEnabled(string executablePath)
        {
            ThrowIfConfigured();
            CheckedPath = executablePath;
            return Enabled;
        }

        public void Enable(string executablePath)
        {
            ThrowIfConfigured();
            EnabledPath = executablePath;
        }

        public void Disable()
        {
            ThrowIfConfigured();
            Disabled = true;
        }

        private void ThrowIfConfigured()
        {
            if (Exception is not null)
            {
                throw Exception;
            }
        }
    }
}

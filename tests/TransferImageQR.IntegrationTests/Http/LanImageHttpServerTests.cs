using System.Net;
using System.Net.Http.Headers;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Infrastructure.Http;
using Xunit;

namespace TransferImageQR.IntegrationTests.Http;

public sealed class LanImageHttpServerTests
{
    private static readonly DateTimeOffset SessionCreatedAt =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartAndStopAsync_BindsAllInterfacesOnDynamicPort()
    {
        var sessionProvider = CreateSessionProvider(new TransferDraft(), new MutableTimeProvider(SessionCreatedAt));
        await using var server = new LanImageHttpServer(sessionProvider);

        await server.StartAsync(TestContext.Current.CancellationToken);

        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);
        Assert.Contains(
            server.BoundAddresses,
            address => address.Contains("0.0.0.0", StringComparison.Ordinal) ||
                       address.Contains("[::]", StringComparison.Ordinal));
        var firstPort = server.Port;

        await server.StartAsync(TestContext.Current.CancellationToken);
        Assert.Equal(firstPort, server.Port);

        await server.StopAsync(TestContext.Current.CancellationToken);
        Assert.False(server.IsRunning);
        await server.StopAsync(TestContext.Current.CancellationToken);

        await server.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);
    }

    [Fact]
    public async Task GetImage_WithActiveSession_ReturnsOriginalBytesAndContentTypes()
    {
        using var files = new TemporaryImageDirectory();
        var expected = new Dictionary<string, (byte[] Bytes, string ContentType)>
        {
            [files.Create("sample.jpg", [0xFF, 0xD8, 0xFF, 0x01])] = ([0xFF, 0xD8, 0xFF, 0x01], "image/jpeg"),
            [files.Create("sample.png", [0x89, 0x50, 0x4E, 0x47])] = ([0x89, 0x50, 0x4E, 0x47], "image/png"),
            [files.Create("sample.webp", [0x52, 0x49, 0x46, 0x46])] = ([0x52, 0x49, 0x46, 0x46], "image/webp"),
        };
        var draft = new TransferDraft();
        var images = expected.Keys.Select(draft.Add).ToArray();
        var timeProvider = new MutableTimeProvider(SessionCreatedAt);
        var sessionProvider = CreateSessionProvider(draft, timeProvider);
        var session = sessionProvider.Create().Session;
        Assert.NotNull(session);
        await using var server = new LanImageHttpServer(sessionProvider);
        await server.StartAsync(TestContext.Current.CancellationToken);
        using var client = CreateClient(server.Port);

        var sessionResponse = await client.GetAsync(
            $"/transfer/{session.Token}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        for (var index = 0; index < images.Length; index++)
        {
            var response = await client.GetAsync(
                $"/transfer/{session.Token}/images/{images[index].Id}",
                TestContext.Current.CancellationToken);
            var expectation = expected[images[index].FilePath];
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectation.ContentType, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(
                expectation.Bytes,
                await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task GetImage_WithoutActiveMatchingToken_ReturnsNotFound()
    {
        using var files = new TemporaryImageDirectory();
        var draft = new TransferDraft();
        var image = draft.Add(files.Create("sample.jpg", [1, 2, 3]));
        var timeProvider = new MutableTimeProvider(SessionCreatedAt);
        var sessionProvider = CreateSessionProvider(draft, timeProvider);
        var session = sessionProvider.Create().Session;
        Assert.NotNull(session);
        await using var server = new LanImageHttpServer(sessionProvider);
        await server.StartAsync(TestContext.Current.CancellationToken);
        using var client = CreateClient(server.Port);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/transfer", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"/transfer/wrong-token/images/{image.Id}",
                TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"/transfer/{session.Token}/images/{Guid.NewGuid()}",
                TestContext.Current.CancellationToken)).StatusCode);

        timeProvider.UtcNow = session.ExpiresAt;
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"/transfer/{session.Token}",
                TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"/transfer/{session.Token}/images/{image.Id}",
                TestContext.Current.CancellationToken)).StatusCode);
    }

    private static TransferSessionUseCase CreateSessionProvider(
        TransferDraft draft,
        TimeProvider timeProvider) =>
        new(draft, new FixedTokenGenerator(), timeProvider);

    private static HttpClient CreateClient(int port) =>
        new()
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders =
            {
                UserAgent = { new ProductInfoHeaderValue("TransferImageQR-IntegrationTests", "1.0") },
            },
        };

    private sealed class FixedTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => "test-token-which-is-not-used-outside-tests";
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class TemporaryImageDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(
            Path.GetTempPath(),
            "TransferImageQR.IntegrationTests",
            Guid.NewGuid().ToString("N"));

        public TemporaryImageDirectory() => Directory.CreateDirectory(_path);

        public string Create(string fileName, byte[] bytes)
        {
            var path = Path.Combine(_path, fileName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public void Dispose() => Directory.Delete(_path, recursive: true);
    }
}

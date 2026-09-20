using System.Net;
using SkiaSharp;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Domain.Drafts;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Infrastructure.Files;
using TransferImageQR.Infrastructure.Http;
using TransferImageQR.Infrastructure.Images;
using TransferImageQR.Infrastructure.QrCodes;
using Xunit;

namespace TransferImageQR.AcceptanceTests;

public sealed class MvpAcceptanceTests
{
    private static readonly DateTimeOffset SessionCreatedAt =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MainFlow_FromRealImagesThroughQrAndHttp_RejectsChangesAndExpires()
    {
        using var files = new TemporaryImageDirectory();
        var jpeg = files.CreateImage("photo.jpg", SKEncodedImageFormat.Jpeg);
        var png = files.CreateImage("graphic.png", SKEncodedImageFormat.Png);
        var webp = files.CreateImage("sticker.webp", SKEncodedImageFormat.Webp);
        var expected = new Dictionary<string, byte[]>
        {
            [jpeg.Path] = jpeg.Bytes,
            [png.Path] = png.Bytes,
            [webp.Path] = webp.Bytes,
        };
        var draft = new TransferDraft();
        var addImages = new AddImagesToDraftUseCase(
            draft,
            new SkiaImageThumbnailProvider(),
            new PhysicalFileMetadataProvider());

        var added = await addImages.ExecuteAsync(
            expected.Keys.ToArray(),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, added.TotalCount);
        Assert.Empty(added.RejectedImages);
        var time = new MutableTimeProvider(SessionCreatedAt);
        var sessions = new TransferSessionUseCase(draft, new FixedTokenGenerator(), time);
        var created = sessions.Create();
        Assert.True(created.Success);
        Assert.NotNull(created.Session);
        await using var server = new LanImageHttpServer(sessions);
        await server.StartAsync(TestContext.Current.CancellationToken);
        var qrService = new TransferQrCodeService(
            new TransferUrlProvider(server),
            new QrCoderPngGenerator());
        var qr = qrService.Create(created.Session.Token, IPAddress.Loopback);
        Assert.NotNull(qr);
        Assert.NotEmpty(qr.PngBytes);
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };

        var gallery = await client.GetAsync(qr.Url.PathAndQuery, TestContext.Current.CancellationToken);
        var galleryHtml = await gallery.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, gallery.StatusCode);
        Assert.Contains("3枚の画像", galleryHtml);

        foreach (var image in created.Session.Images)
        {
            var response = await client.GetAsync(
                $"/transfer/{created.Session.Token}/images/{image.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                expected[image.FilePath],
                await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        }

        var blocked = await addImages.ExecuteAsync(
            [expected.Keys.First()],
            TestContext.Current.CancellationToken);
        Assert.Equal(DraftImageRejectionReason.DraftNotEditable, Assert.Single(blocked.RejectedImages).Reason);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/transfer/invalid-token", TestContext.Current.CancellationToken)).StatusCode);

        time.UtcNow = created.Session.ExpiresAt;
        Assert.Equal(
            HttpStatusCode.Gone,
            (await client.GetAsync(qr.Url.PathAndQuery, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Gone,
            (await client.GetAsync(
                $"/transfer/{created.Session.Token}/images/{created.Session.Images[0].Id}",
                TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task InputValidation_WithRealFiles_RejectsOversizeUnsupportedAndTwentyFirstImage()
    {
        using var files = new TemporaryImageDirectory();
        var oversized = files.CreateBytes("oversized.jpg", new byte[(10 * 1024 * 1024) + 1]);
        var unsupported = files.CreateBytes("notes.txt", [1, 2, 3]);
        var draft = new TransferDraft();
        var addImages = new AddImagesToDraftUseCase(
            draft,
            new SkiaImageThumbnailProvider(),
            new PhysicalFileMetadataProvider());

        var invalid = await addImages.ExecuteAsync(
            [oversized, unsupported],
            TestContext.Current.CancellationToken);

        Assert.Collection(
            invalid.RejectedImages,
            rejection => Assert.Equal(DraftImageRejectionReason.FileTooLarge, rejection.Reason),
            rejection => Assert.Equal(DraftImageRejectionReason.UnsupportedFormat, rejection.Reason));

        var candidates = Enumerable.Range(1, 21)
            .Select(index => files.CreateImage($"image-{index:D2}.png", SKEncodedImageFormat.Png).Path)
            .ToArray();
        var capacity = await addImages.ExecuteAsync(candidates, TestContext.Current.CancellationToken);

        Assert.Equal(20, capacity.TotalCount);
        Assert.Equal(20, capacity.AddedImages.Count);
        var twentyFirst = Assert.Single(capacity.RejectedImages);
        Assert.Equal("image-21.png", twentyFirst.FileName);
        Assert.Equal(DraftImageRejectionReason.DraftLimitReached, twentyFirst.Reason);
    }

    private sealed class FixedTokenGenerator : ISessionTokenGenerator
    {
        public string Generate() => "acceptance-test-token-with-sufficient-entropy";
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
            "TransferImageQR.AcceptanceTests",
            Guid.NewGuid().ToString("N"));

        public TemporaryImageDirectory() => Directory.CreateDirectory(_path);

        public (string Path, byte[] Bytes) CreateImage(string fileName, SKEncodedImageFormat format)
        {
            using var bitmap = new SKBitmap(24, 18);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.CornflowerBlue);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(format, quality: 90);
            var bytes = data.ToArray();
            return (CreateBytes(fileName, bytes), bytes);
        }

        public string CreateBytes(string fileName, byte[] bytes)
        {
            var path = Path.Combine(_path, fileName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public void Dispose() => Directory.Delete(_path, recursive: true);
    }
}

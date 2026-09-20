using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Application.Transfers;

namespace TransferImageQR.Infrastructure.Http;

public sealed class LanImageHttpServer(
    ITransferSessionAccessProvider sessionProvider,
    int requestedPort = 0) : IAsyncDisposable, IHttpServerEndpoint
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private WebApplication? _application;
    private string[] _boundAddresses = [];

    public bool IsRunning => _application is not null;

    public int Port { get; private set; }

    public IReadOnlyList<string> BoundAddresses => _boundAddresses;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_application is not null)
            {
                return;
            }

            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(requestedPort));

            var application = builder.Build();
            MapEndpoints(application);

            try
            {
                await application.StartAsync(cancellationToken);
                var addresses = application.Services
                    .GetRequiredService<IServer>()
                    .Features
                    .Get<IServerAddressesFeature>()?
                    .Addresses
                    .ToArray() ?? [];
                var address = addresses.FirstOrDefault()
                    ?? throw new InvalidOperationException("Kestrel did not report a bound address.");

                Port = new Uri(address).Port;
                _boundAddresses = addresses;
                _application = application;
            }
            catch
            {
                await application.DisposeAsync();
                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            var application = _application;
            if (application is null)
            {
                return;
            }

            _application = null;
            Port = 0;
            _boundAddresses = [];

            try
            {
                await application.StopAsync(cancellationToken);
            }
            finally
            {
                await application.DisposeAsync();
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lifecycleGate.Dispose();
    }

    private void MapEndpoints(WebApplication application)
    {
        application.MapGet(
            "/transfer/{token}",
            (HttpContext context, string token) => GetSession(context, token));
        application.MapGet(
            "/transfer/{token}/images/{imageId:guid}",
            (HttpContext context, string token, Guid imageId) => GetImage(context, token, imageId));
    }

    private IResult GetSession(HttpContext context, string token)
    {
        context.Response.Headers.CacheControl = "no-store";
        var access = sessionProvider.GetAccess(token);
        return access.Status switch
        {
            TransferSessionAccessStatus.Active when access.Session is not null => Results.Content(
                TransferGalleryPageRenderer.Render(access.Session),
                "text/html; charset=utf-8"),
            TransferSessionAccessStatus.Expired => ExpiredPage(),
            _ => Results.NotFound(),
        };
    }

    private IResult GetImage(HttpContext context, string token, Guid imageId)
    {
        context.Response.Headers.CacheControl = "no-store";
        var access = sessionProvider.GetAccess(token);
        if (access.Status == TransferSessionAccessStatus.Expired)
        {
            return ExpiredPage();
        }

        var image = access.Session?.Images.FirstOrDefault(candidate => candidate.Id == imageId);
        if (image is null)
        {
            return Results.NotFound();
        }

        if (!File.Exists(image.FilePath) || !TryGetContentType(image.FilePath, out var contentType))
        {
            return Results.Content(
                TransferImageUnavailablePageRenderer.Render(),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status404NotFound);
        }

        var contentDisposition = new ContentDispositionHeaderValue("inline")
        {
            FileNameStar = image.FileName,
        };
        context.Response.Headers.ContentDisposition = contentDisposition.ToString();

        return Results.File(
            image.FilePath,
            contentType,
            enableRangeProcessing: true);
    }

    private static IResult ExpiredPage() =>
        Results.Content(
            TransferExpiredPageRenderer.Render(),
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status410Gone);

    private static bool TryGetContentType(string filePath, out string contentType)
    {
        contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => string.Empty,
        };

        return contentType.Length > 0;
    }
}

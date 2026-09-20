using System.Reflection;

namespace TransferImageQR.Presentation;

internal static class ApplicationFonts
{
    internal const string ExpectedFamilyName = "Noto Sans JP";
    internal const string ResourceName = "TransferImageQR.Assets.Fonts.NotoSansJP.ttf";

    private static readonly object SyncRoot = new();
    private static ApplicationFontManager? _current;

    public static void Initialize(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        lock (SyncRoot)
        {
            _current ??= ApplicationFontManager.Create(ReadEmbeddedFontBytes(assembly));
        }
    }

    public static void ApplyTo(Control root)
    {
        EnsureInitialized();
        _current!.ApplyTo(root);
    }

    public static byte[]? ReadEmbeddedFontBytes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public static void Shutdown()
    {
        lock (SyncRoot)
        {
            _current?.Dispose();
            _current = null;
        }
    }

    private static void EnsureInitialized() =>
        Initialize(typeof(ApplicationFonts).Assembly);
}

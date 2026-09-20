using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace TransferImageQR.Presentation;

internal sealed class ApplicationFontManager : IDisposable
{
    private readonly PrivateFontCollection? _privateFonts;
    private readonly IntPtr _fontMemory;
    private readonly Dictionary<(float Size, FontStyle Style, GraphicsUnit Unit), Font> _sharedFonts = [];
    private readonly object _sharedFontsLock = new();
    private bool _disposed;

    private ApplicationFontManager(
        FontFamily family,
        PrivateFontCollection? privateFonts,
        IntPtr fontMemory,
        bool isEmbeddedFontLoaded)
    {
        Family = family;
        _privateFonts = privateFonts;
        _fontMemory = fontMemory;
        IsEmbeddedFontLoaded = isEmbeddedFontLoaded;
    }

    public FontFamily Family { get; }

    public bool IsEmbeddedFontLoaded { get; }

    public static ApplicationFontManager Create(byte[]? fontBytes)
    {
        if (fontBytes is null || fontBytes.Length == 0)
        {
            return CreateFallback();
        }

        PrivateFontCollection? privateFonts = null;
        var fontMemory = IntPtr.Zero;
        try
        {
            fontMemory = Marshal.AllocCoTaskMem(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, fontMemory, fontBytes.Length);
            privateFonts = new PrivateFontCollection();
            privateFonts.AddMemoryFont(fontMemory, fontBytes.Length);
            var family = privateFonts.Families.FirstOrDefault(
                candidate => string.Equals(
                    candidate.Name,
                    ApplicationFonts.ExpectedFamilyName,
                    StringComparison.Ordinal));
            if (family is null)
            {
                privateFonts.Dispose();
                Marshal.FreeCoTaskMem(fontMemory);
                return CreateFallback();
            }

            return new ApplicationFontManager(family, privateFonts, fontMemory, true);
        }
        catch (Exception exception) when (
            exception is ArgumentException or ExternalException)
        {
            privateFonts?.Dispose();
            if (fontMemory != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(fontMemory);
            }

            return CreateFallback();
        }
    }

    public Font CreateFont(
        float size,
        FontStyle style,
        GraphicsUnit unit = GraphicsUnit.Point)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return CreateFontCore(size, style, unit);
    }

    public void ApplyTo(Control root)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(root);
        ApplyToControl(root);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sharedFontsLock)
        {
            foreach (var font in _sharedFonts.Values)
            {
                font.Dispose();
            }

            _sharedFonts.Clear();
        }

        _privateFonts?.Dispose();
        if (_fontMemory != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_fontMemory);
        }

        _disposed = true;
    }

    private static ApplicationFontManager CreateFallback()
    {
        var fallbackFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        return new ApplicationFontManager(
            fallbackFont.FontFamily,
            null,
            IntPtr.Zero,
            false);
    }

    private void ApplyToControl(Control control)
    {
        control.Font = GetSharedFont(control.Font.Size, control.Font.Style, control.Font.Unit);
        if (control is ToolStrip toolStrip)
        {
            foreach (ToolStripItem item in toolStrip.Items)
            {
                ApplyToToolStripItem(item);
            }
        }

        foreach (Control child in control.Controls)
        {
            ApplyToControl(child);
        }
    }

    private void ApplyToToolStripItem(ToolStripItem item)
    {
        item.Font = GetSharedFont(item.Font.Size, item.Font.Style, item.Font.Unit);
        if (item is not ToolStripDropDownItem dropDownItem)
        {
            return;
        }

        foreach (ToolStripItem child in dropDownItem.DropDownItems)
        {
            ApplyToToolStripItem(child);
        }
    }

    private Font GetSharedFont(float size, FontStyle style, GraphicsUnit unit)
    {
        lock (_sharedFontsLock)
        {
            var key = (size, style, unit);
            if (!_sharedFonts.TryGetValue(key, out var font))
            {
                font = CreateFontCore(size, style, unit);
                _sharedFonts.Add(key, font);
            }

            return font;
        }
    }

    private Font CreateFontCore(float size, FontStyle style, GraphicsUnit unit)
    {
        try
        {
            return new Font(Family, size, style, unit);
        }
        catch (ArgumentException)
        {
            return new Font(Family, size, FontStyle.Regular, unit);
        }
    }
}

using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace PhotoEditor.Maui;

internal static class SkiaPhotoEditorTextRenderer
{
    private static string? PersianFontAsset => PhotoEditorTextFontOptions.Current.RtlFontAssetFileName;
    private static string? LatinFontAsset => PhotoEditorTextFontOptions.Current.LatinFontAssetFileName;

    private static readonly object LoadLock = new();
    private static SKTypeface? _persianTypeface;
    private static SKTypeface? _latinTypeface;

    public static SKTypeface GetTypeface(string? text)
    {
        if (PrefersPersianTypeface(text))
            return GetPersianTypeface();

        return GetLatinTypeface();
    }

    public static float GetLineHeight(float fontSize, string? sampleText = null)
    {
        using var font = new SKFont(GetTypeface(sampleText), fontSize);
        var metrics = font.Metrics;
        return metrics.Descent - metrics.Ascent;
    }

    public static (float Width, float Height) Measure(string? text, float fontSize)
    {
        var lineHeight = GetLineHeight(fontSize, text);
        if (string.IsNullOrEmpty(text))
            return (0, lineHeight);

        var typeface = GetTypeface(text);
        using var font = new SKFont(typeface, fontSize);
        using var shaper = new SKShaper(typeface);
        var result = shaper.Shape(text, font);
        return (Math.Max(result.Width, fontSize * 0.25f), lineHeight);
    }

    public static void DrawAtCenter(
        SKCanvas canvas,
        string text,
        float centerX,
        float centerY,
        float fontSize,
        SKColor color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var typeface = GetTypeface(text);
        using var font = new SKFont(typeface, fontSize);
        var metrics = font.Metrics;
        var baselineY = centerY - (metrics.Ascent + metrics.Descent) / 2f;
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        using var shaper = new SKShaper(typeface);
        canvas.DrawShapedText(shaper, text, centerX, baselineY, SKTextAlign.Center, font, paint);
    }

    private static bool PrefersPersianTypeface(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            foreach (var c in text)
            {
                if (IsRtlScriptChar(c))
                    return true;
            }

            return false;
        }

        var preferRtl = PhotoEditorTextFontOptions.Current.PreferRtlTypeface;
        if (preferRtl is not null)
            return preferRtl();

        return Application.Current?.Windows.FirstOrDefault()?.Page?.FlowDirection == FlowDirection.RightToLeft;
    }

    private static bool IsRtlScriptChar(char c) =>
        c is >= '\u0600' and <= '\u06FF'
        or >= '\u0750' and <= '\u077F'
        or >= '\u08A0' and <= '\u08FF'
        or >= '\uFB50' and <= '\uFDFF'
        or >= '\uFE70' and <= '\uFEFF';

    private static SKTypeface GetPersianTypeface() =>
        _persianTypeface ??= LoadTypeface(PersianFontAsset)
                          ?? SKFontManager.Default.MatchCharacter('\u06AF')
                          ?? SKTypeface.Default;

    private static SKTypeface GetLatinTypeface() =>
        _latinTypeface ??= LoadTypeface(LatinFontAsset) ?? SKTypeface.Default;

    private static SKTypeface? LoadTypeface(string? assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return null;

        lock (LoadLock)
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(assetName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                return SKTypeface.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}

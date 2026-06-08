using SkiaSharp;

namespace PhotoEditor.Maui;

internal static class SkiaColorExtensions
{
    public static SKColor ToSkColor(this Color color) =>
        new(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255));
}

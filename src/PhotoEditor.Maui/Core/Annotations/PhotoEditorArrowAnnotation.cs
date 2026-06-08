using SkiaSharp;

namespace PhotoEditor.Maui;

public sealed class PhotoEditorArrowAnnotation(SKColor color, float width)
{
    public SKColor Color { get; } = color;
    public float Width { get; set; } = width;
    public List<SKPoint> Points { get; } = [];
    public float HeadRevealProgress { get; set; } = 1f;
}

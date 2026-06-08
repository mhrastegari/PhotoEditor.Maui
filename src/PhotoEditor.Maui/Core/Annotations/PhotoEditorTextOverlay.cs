using SkiaSharp;

namespace PhotoEditor.Maui;

internal sealed class PhotoEditorTextOverlay(string text, SKPoint position, SKColor color, float fontSize)
{
    public string Text { get; } = text;
    public SKPoint Position { get; } = position;
    public SKColor Color { get; } = color;
    public float FontSize { get; } = fontSize;
}

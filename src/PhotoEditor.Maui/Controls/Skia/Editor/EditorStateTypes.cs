using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private sealed class EditSnapshot(
        SKBitmap bitmap,
        List<StrokePath> strokes,
        List<PhotoEditorArrowAnnotation> arrows,
        List<PhotoEditorTextOverlay> textOverlays) : IDisposable
    {
        public SKBitmap Bitmap { get; } = bitmap;
        public List<StrokePath> Strokes { get; } = strokes;
        public List<PhotoEditorArrowAnnotation> Arrows { get; } = arrows;
        public List<PhotoEditorTextOverlay> TextOverlays { get; } = textOverlays;

        public void Dispose() => Bitmap.Dispose();
    }

    private sealed class StrokePath(SKColor color, float width)
    {
        public SKColor Color { get; set; } = color;
        public float Width { get; set; } = width;
        public List<SKPoint> Points { get; } = [];
    }

    private enum CropDragHandle
    {
        None = 0,
        Move = 1,
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 4,
        BottomRight = 5,
        Top = 6,
        Bottom = 7,
        Left = 8,
        Right = 9
    }
}
using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void HandleDrawTouch(SKTouchEventArgs e, SKPoint imagePoint)
    {
        var imageBounds = GetImageBounds();
        if (imageBounds.IsEmpty) return;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!imageBounds.Contains(imagePoint.X, imagePoint.Y))
                    return;

                _redoStrokes.Clear();
                ClearEditRedoStack();
                _activeStroke = new StrokePath(StrokeColor.ToSkColor(), ViewSizeToBitmapSize(DrawStrokeWidth));
                _activeStroke.Points.Add(ClampPointToImageBounds(imagePoint, imageBounds));
                _strokes.Add(_activeStroke);
                CanvasView.InvalidateSurface();
                RaiseDrawingHistoryChanged();
                break;

            case SKTouchAction.Moved:
                if (_activeStroke is null)
                    break;

                _activeStroke.Points.Add(ClampPointToImageBounds(imagePoint, imageBounds));
                CanvasView.InvalidateSurface();
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                _activeStroke = null;
                break;
        }
    }
    private void DrawStrokes(SKCanvas canvas)
    {
        if (_imageDestView.Width <= 0.1f || _imageDestView.Height <= 0.1f)
            return;

        canvas.Save();
        canvas.ClipRect(_imageDestView);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        foreach (var stroke in _strokes)
        {
            if (stroke.Points.Count < 2)
                continue;

            paint.Color = stroke.Color;
            paint.StrokeWidth = BitmapSizeToViewSize(stroke.Width);

            using var path = new SKPath();
            var first = BitmapToView(stroke.Points[0]);
            path.MoveTo(first);
            for (var i = 1; i < stroke.Points.Count; i++)
                path.LineTo(BitmapToView(stroke.Points[i]));

            canvas.DrawPath(path, paint);
        }

        canvas.Restore();
    }
}
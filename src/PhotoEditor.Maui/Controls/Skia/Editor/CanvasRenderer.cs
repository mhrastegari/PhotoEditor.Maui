using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_isMutatingBitmap)
            return;

        SKBitmap? bitmap;
        int bitmapWidth;
        int bitmapHeight;
        lock (_bitmapLock)
        {
            bitmap = _bitmap;
            if (bitmap is null || bitmap.IsNull)
                return;

            bitmapWidth = bitmap.Width;
            bitmapHeight = bitmap.Height;
        }

        UpdateBitmapDisplayMatrix(e.Info.Width, e.Info.Height, bitmapWidth, bitmapHeight);

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Crop && _croppingRectNeedsSync)
        {
            _croppingRect = new CroppingRectangle(SKRect.Create(0, 0, bitmapWidth, bitmapHeight));
            _croppingRectNeedsSync = false;
        }

        lock (_bitmapLock)
        {
            if (_bitmap is null || _bitmap.IsNull)
                return;

            canvas.DrawBitmap(_bitmap, _imageDestView);
        }

        DrawStrokes(canvas);
        DrawArrows(canvas);
        DrawTextOverlays(canvas);
        DrawPendingTextPreview(canvas);

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Crop && _croppingRect is not null)
        {
            var scaledCropRect = _bitmapMatrix.MapRect(_croppingRect.Rect);
            DrawCropOverlay(canvas, _imageDestView, scaledCropRect);
        }
    }

    private void FlattenOverlaysIntoBitmap()
    {
        if (_strokes.Count == 0 && _arrows.Count == 0 && _textOverlays.Count == 0)
            return;

        var composite = RenderCompositeBitmap();
        if (composite is null)
            return;

        lock (_bitmapLock)
        {
            _bitmap?.Dispose();
            _bitmap = composite;
        }

        _strokes.Clear();
        _redoStrokes.Clear();
        _arrows.Clear();
        _redoArrows.Clear();
        _textOverlays.Clear();
        _textRedoOverlays.Clear();
        _activeStroke = null;
        _activeArrow = null;
        HideTextInputHost();
        RaiseDrawingHistoryChanged();
    }

    private SKBitmap? RenderCompositeBitmap()
    {
        SKBitmap? bitmap;
        lock (_bitmapLock)
            bitmap = _bitmap?.Copy();

        if (bitmap is null || bitmap.IsNull)
            return null;

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface is null)
            return null;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(bitmap, 0, 0);
        bitmap.Dispose();

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
            paint.StrokeWidth = stroke.Width;

            using var path = new SKPath();
            path.MoveTo(stroke.Points[0]);
            for (var i = 1; i < stroke.Points.Count; i++)
                path.LineTo(stroke.Points[i]);

            canvas.DrawPath(path, paint);
        }

        var headLimitScale = 1f / GetBitmapDisplayScale();

        foreach (var arrow in _arrows)
        {
            if (arrow.Points.Count < 2)
                continue;

            SkiaPhotoEditorArrowRenderer.DrawPathWithArrowHead(
                canvas,
                arrow.Points,
                arrow.Color,
                arrow.Width,
                headLimitScale: headLimitScale);
        }

        foreach (var overlay in _textOverlays)
        {
            if (string.IsNullOrWhiteSpace(overlay.Text))
                continue;

            SkiaPhotoEditorTextRenderer.DrawAtCenter(
                canvas,
                overlay.Text,
                overlay.Position.X,
                overlay.Position.Y,
                overlay.FontSize,
                overlay.Color);
        }

        canvas.Flush();
        using var image = surface.Snapshot();
        var result = SKBitmap.FromImage(image);
        if (result is null)
            return null;

        if (result.ColorType == SKColorType.Rgba8888 && result.AlphaType is SKAlphaType.Premul or SKAlphaType.Unpremul)
            return result;

        return SkiaPhotoEditorBitmapHelper.NormalizeBitmap(result);
    }
}

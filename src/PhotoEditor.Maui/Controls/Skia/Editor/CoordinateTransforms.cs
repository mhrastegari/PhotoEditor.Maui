using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private float GetBitmapDisplayScale()
    {
        var scale = Math.Abs(_bitmapMatrix.ScaleX);
        return scale > 0.001f ? scale : 1f;
    }

    private float ViewSizeToBitmapSize(float viewPixels)
    {
        var scale = GetBitmapDisplayScale();
        return scale > 0.001f ? viewPixels / scale : viewPixels;
    }

    private float BitmapSizeToViewSize(float bitmapPixels) =>
        bitmapPixels * GetBitmapDisplayScale();

    private double GetCanvasToLayoutScaleX()
    {
        var canvasW = _surfaceWidth > 0 ? _surfaceWidth : CanvasView.CanvasSize.Width;
        var layoutW = CanvasView.Width > 0 ? CanvasView.Width : EditorRoot.Width;
        return canvasW > 0.5 && layoutW > 0 ? layoutW / canvasW : 1.0;
    }

    private void UpdateBitmapDisplayMatrix(int canvasWidth, int canvasHeight, int imageWidth, int imageHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
            return;

        _surfaceWidth = canvasWidth;
        _surfaceHeight = canvasHeight;

        var scale = Math.Min((float)canvasWidth / imageWidth, (float)canvasHeight / imageHeight);
        var x = (canvasWidth - scale * imageWidth) / 2f;
        var y = (canvasHeight - scale * imageHeight) / 2f;

        _bitmapMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(x, y), SKMatrix.CreateScale(scale, scale));
        _bitmapMatrix.TryInvert(out _inverseBitmapMatrix);

        _imageDestView = _bitmapMatrix.MapRect(SKRect.Create(0, 0, imageWidth, imageHeight));

        var displayScale = GetBitmapDisplayScale();
        if (TextInputHost.IsVisible && Math.Abs(displayScale - _lastBitmapDisplayScale) > 0.001f)
        {
            _lastBitmapDisplayScale = displayScale;
            Dispatcher.Dispatch(RefreshVisibleTextInputHost);
        }
        else
        {
            _lastBitmapDisplayScale = displayScale;
        }
    }

    private SKPoint GetCanvasTouchPixel(SKTouchEventArgs e)
    {
        var pixel = new SKPoint((float)e.Location.X, (float)e.Location.Y);
        var canvasW = CanvasView.CanvasSize.Width;
        var canvasH = CanvasView.CanvasSize.Height;

        if (_surfaceWidth > 0 && _surfaceHeight > 0)
        {
            if (canvasW > 0.5f && Math.Abs(canvasW - _surfaceWidth) > 0.5f)
                pixel.X *= _surfaceWidth / canvasW;
            if (canvasH > 0.5f && Math.Abs(canvasH - _surfaceHeight) > 0.5f)
                pixel.Y *= _surfaceHeight / canvasH;
        }

        return pixel;
    }

    private SKPoint ViewToBitmap(SKPoint canvasPixel) => _inverseBitmapMatrix.MapPoint(canvasPixel);

    private SKPoint BitmapToView(SKPoint bitmapPoint) => _bitmapMatrix.MapPoint(bitmapPoint);

    private Point CanvasPixelToLayoutPoint(SKPoint canvasPixel)
    {
        var canvasW = _surfaceWidth > 0 ? _surfaceWidth : CanvasView.CanvasSize.Width;
        var canvasH = _surfaceHeight > 0 ? _surfaceHeight : CanvasView.CanvasSize.Height;
        var layoutW = CanvasView.Width > 0 ? CanvasView.Width : EditorRoot.Width;
        var layoutH = CanvasView.Height > 0 ? CanvasView.Height : EditorRoot.Height;

        if (canvasW <= 0.5 || canvasH <= 0.5 || layoutW <= 0 || layoutH <= 0)
            return new Point(canvasPixel.X, canvasPixel.Y);

        return new Point(
            canvasPixel.X * layoutW / canvasW,
            canvasPixel.Y * layoutH / canvasH);
    }

    private SKPoint LayoutPointToCanvasPixel(Point layoutPoint)
    {
        var canvasW = _surfaceWidth > 0 ? _surfaceWidth : CanvasView.CanvasSize.Width;
        var canvasH = _surfaceHeight > 0 ? _surfaceHeight : CanvasView.CanvasSize.Height;
        var layoutW = CanvasView.Width > 0 ? CanvasView.Width : EditorRoot.Width;
        var layoutH = CanvasView.Height > 0 ? CanvasView.Height : EditorRoot.Height;

        if (canvasW <= 0.5 || canvasH <= 0.5 || layoutW <= 0 || layoutH <= 0)
            return new SKPoint((float)layoutPoint.X, (float)layoutPoint.Y);

        return new SKPoint(
            (float)(layoutPoint.X * canvasW / layoutW),
            (float)(layoutPoint.Y * canvasH / layoutH));
    }

    private SKRect GetImageBounds()
    {
        lock (_bitmapLock)
        {
            if (_bitmap is null || _bitmap.IsNull)
                return SKRect.Empty;

            return SKRect.Create(0, 0, _bitmap.Width, _bitmap.Height);
        }
    }

    private static SKPoint ClampPointToImageBounds(SKPoint point, SKRect imageBounds) =>
        new(
            Math.Clamp(point.X, imageBounds.Left, imageBounds.Right),
            Math.Clamp(point.Y, imageBounds.Top, imageBounds.Bottom));

    private SKPoint ClampPointToBitmapBounds(SKPoint point) =>
        ClampPointToImageBounds(point, GetImageBounds());

    private void EnsureLayoutForTouch()
    {
        if (_surfaceWidth > 0 && _imageDestView.Width > 0.1f)
            return;

        CanvasView.InvalidateSurface();
    }

    private static SKRect ClampRectToBounds(SKRect rect, SKRect bounds)
    {
        var left = Math.Clamp(rect.Left, bounds.Left, bounds.Right);
        var top = Math.Clamp(rect.Top, bounds.Top, bounds.Bottom);
        var right = Math.Clamp(rect.Right, bounds.Left, bounds.Right);
        var bottom = Math.Clamp(rect.Bottom, bounds.Top, bounds.Bottom);

        if (right - left < 1f)
            right = left + 1f;
        if (bottom - top < 1f)
            bottom = top + 1f;

        right = Math.Min(right, bounds.Right);
        bottom = Math.Min(bottom, bounds.Bottom);
        left = Math.Max(left, bounds.Left);
        top = Math.Max(top, bounds.Top);

        return SKRect.Create(left, top, right - left, bottom - top);
    }
}
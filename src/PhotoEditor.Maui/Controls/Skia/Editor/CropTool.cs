using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private Task ApplyCropAsyncCore()
    {
        IsLoading = true;
        _isMutatingBitmap = true;
        try
        {
            SKRect cropRect;
            lock (_bitmapLock)
            {
                if (_bitmap is null || _bitmap.IsNull || _croppingRect is null)
                    return Task.CompletedTask;

                cropRect = _croppingRect.Rect;
            }

            PushEditHistory();
            FlattenOverlaysIntoBitmap();

            SKBitmap? sourceCopy;
            lock (_bitmapLock)
            {
                if (_bitmap is null || _bitmap.IsNull)
                    return Task.CompletedTask;

                sourceCopy = _bitmap.Copy();
            }

            if (sourceCopy is null || sourceCopy.IsNull)
                return Task.CompletedTask;

            SKBitmap? cropped;
            try
            {
                cropped = SkiaPhotoEditorBitmapHelper.CropBitmapFromRect(sourceCopy, cropRect);
            }
            finally
            {
                sourceCopy.Dispose();
            }

            if (cropped is null || cropped.IsNull)
                return Task.CompletedTask;

            lock (_bitmapLock)
            {
                _bitmap?.Dispose();
                _bitmap = cropped;
            }

            _croppingRectNeedsSync = true;
            InteractionMode = SkiaPhotoEditorInteractionMode.None;
            _activeHandle = CropDragHandle.None;
            RaiseDrawingHistoryChanged();
        }
        finally
        {
            _isMutatingBitmap = false;
            IsLoading = false;
            CanvasView.InvalidateSurface();
            RaiseInteractionModeChanged();
        }
        return Task.CompletedTask;
    }

    private Task RotateCropClockwiseAsyncCore()
    {
        IsLoading = true;
        _isMutatingBitmap = true;
        try
        {
            PushEditHistory();
            FlattenOverlaysIntoBitmap();

            SKBitmap? sourceCopy;
            lock (_bitmapLock)
            {
                if (_bitmap is null || _bitmap.IsNull)
                    return Task.CompletedTask;

                sourceCopy = _bitmap.Copy();
            }

            if (sourceCopy is null || sourceCopy.IsNull)
                return Task.CompletedTask;

            var rotated = SkiaPhotoEditorBitmapHelper.RotateBitmap(sourceCopy, 90);
            lock (_bitmapLock)
            {
                _bitmap?.Dispose();
                _bitmap = rotated;
            }

            _croppingRectNeedsSync = true;
            _activeHandle = CropDragHandle.None;
            RaiseDrawingHistoryChanged();
        }
        finally
        {
            _isMutatingBitmap = false;
            IsLoading = false;
            CanvasView.InvalidateSurface();
        }

        return Task.CompletedTask;
    }

    private void HandleCropTouch(SKTouchEventArgs e)
    {
        if (_croppingRect is null)
            return;

        var pixelLocation = GetCanvasTouchPixel(e);
        var bitmapLocation = ViewToBitmap(pixelLocation);
        var cropView = _bitmapMatrix.MapRect(_croppingRect.Rect);
        var cropOptions = EditorOptions.Canvas.Crop;
        var borderTolerance = cropOptions.BorderHitToleranceViewPx;
        var cropFillsImage = NearlyEqual(_croppingRect.Rect, _croppingRect.MaxBounds);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _activeHandle = HitTestCropHandle(
                    pixelLocation,
                    cropView,
                    borderTolerance,
                    cropOptions.HandleHitRadiusViewPx,
                    cropFillsImage);
                if (_activeHandle == CropDragHandle.None)
                    break;

                _cropDragSize = _activeHandle == CropDragHandle.Move
                    ? new SKSize(_croppingRect.Rect.Width, _croppingRect.Rect.Height)
                    : SKSize.Empty;
                _cropTouchOffset = bitmapLocation - GetCropHandlePoint(_croppingRect.Rect, _activeHandle);
                break;

            case SKTouchAction.Moved:
                if (_activeHandle == CropDragHandle.None)
                    break;

                var target = ClampPointToBitmapBounds(bitmapLocation - _cropTouchOffset);
                MoveCropHandle(_croppingRect, _activeHandle, target, _cropDragSize);

                if (_activeHandle == CropDragHandle.Move)
                    _croppingRect.ClampPositionToBounds();

                CanvasView.InvalidateSurface();
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                _activeHandle = CropDragHandle.None;
                _cropDragSize = SKSize.Empty;
                CanvasView.InvalidateSurface();
                break;
        }
    }

    private void DrawCropOverlay(SKCanvas canvas, SKRect imageDest, SKRect cropView)
    {
        cropView = ClampRectToBounds(cropView, imageDest);

        var crop = EditorOptions.Canvas.Crop;
        using var dimPaint = new SKPaint { Color = SKColors.Black.WithAlpha(crop.OverlayDimAlpha), Style = SKPaintStyle.Fill };
        using var borderPaint = new SKPaint
        {
            Color = crop.OverlayBorderColor.ToSkColor(),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = crop.OverlayBorderWidth,
            IsAntialias = true
        };
        using var handlePaint = new SKPaint
        {
            Color = crop.OverlayHandleColor.ToSkColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.Save();
        canvas.ClipRect(imageDest);
        canvas.ClipRect(cropView, SKClipOperation.Difference);
        canvas.DrawRect(imageDest, dimPaint);
        canvas.Restore();

        var inset = borderPaint.StrokeWidth / 2f;
        canvas.DrawRect(
            SKRect.Create(
                cropView.Left + inset,
                cropView.Top + inset,
                Math.Max(0, cropView.Width - borderPaint.StrokeWidth),
                Math.Max(0, cropView.Height - borderPaint.StrokeWidth)),
            borderPaint);

        foreach (var handle in GetCropHandlePoints(cropView))
            canvas.DrawCircle(handle, crop.HandleRadiusViewPx, handlePaint);
    }

    private static bool NearlyEqual(SKRect a, SKRect b, float epsilon = 0.5f) =>
        Math.Abs(a.Left - b.Left) <= epsilon &&
        Math.Abs(a.Top - b.Top) <= epsilon &&
        Math.Abs(a.Right - b.Right) <= epsilon &&
        Math.Abs(a.Bottom - b.Bottom) <= epsilon;

    private static CropDragHandle HitTestCropHandle(
        SKPoint viewPoint,
        SKRect cropView,
        float borderTolerance,
        float cornerHitRadiusViewPx,
        bool cropFillsImage)
    {
        var cornerTolerance = cornerHitRadiusViewPx > 0f
            ? cornerHitRadiusViewPx
            : borderTolerance;

        var corners = new (CropDragHandle Handle, SKPoint Point)[]
        {
            (CropDragHandle.TopLeft, new SKPoint(cropView.Left, cropView.Top)),
            (CropDragHandle.TopRight, new SKPoint(cropView.Right, cropView.Top)),
            (CropDragHandle.BottomRight, new SKPoint(cropView.Right, cropView.Bottom)),
            (CropDragHandle.BottomLeft, new SKPoint(cropView.Left, cropView.Bottom)),
        };

        foreach (var (handle, point) in corners)
        {
            if (Math.Abs(viewPoint.X - point.X) <= cornerTolerance &&
                Math.Abs(viewPoint.Y - point.Y) <= cornerTolerance)
                return handle;
        }

        var edge = HitTestCropEdge(viewPoint, cropView, borderTolerance);
        if (edge != CropDragHandle.None)
            return edge;

        if (cropFillsImage)
            return CropDragHandle.None;

        return cropView.Contains(viewPoint) ? CropDragHandle.Move : CropDragHandle.None;
    }

    private static CropDragHandle HitTestCropEdge(SKPoint viewPoint, SKRect cropView, float tolerance)
    {
        var distTop = Math.Abs(viewPoint.Y - cropView.Top);
        var distBottom = Math.Abs(viewPoint.Y - cropView.Bottom);
        var distLeft = Math.Abs(viewPoint.X - cropView.Left);
        var distRight = Math.Abs(viewPoint.X - cropView.Right);

        var nearTop = distTop <= tolerance &&
                      viewPoint.X >= cropView.Left - tolerance &&
                      viewPoint.X <= cropView.Right + tolerance;
        var nearBottom = distBottom <= tolerance &&
                         viewPoint.X >= cropView.Left - tolerance &&
                         viewPoint.X <= cropView.Right + tolerance;
        var nearLeft = distLeft <= tolerance &&
                       viewPoint.Y >= cropView.Top - tolerance &&
                       viewPoint.Y <= cropView.Bottom + tolerance;
        var nearRight = distRight <= tolerance &&
                        viewPoint.Y >= cropView.Top - tolerance &&
                        viewPoint.Y <= cropView.Bottom + tolerance;

        if (!nearTop && !nearBottom && !nearLeft && !nearRight)
            return CropDragHandle.None;

        var min = float.MaxValue;
        CropDragHandle nearest = CropDragHandle.None;

        if (nearTop && distTop < min)
        {
            min = distTop;
            nearest = CropDragHandle.Top;
        }

        if (nearBottom && distBottom < min)
        {
            min = distBottom;
            nearest = CropDragHandle.Bottom;
        }

        if (nearLeft && distLeft < min)
        {
            min = distLeft;
            nearest = CropDragHandle.Left;
        }

        if (nearRight && distRight < min)
        {
            min = distRight;
            nearest = CropDragHandle.Right;
        }

        return nearest;
    }

    private static IEnumerable<SKPoint> GetCropHandlePoints(SKRect cropView)
    {
        var cx = cropView.MidX;
        var cy = cropView.MidY;

        yield return new SKPoint(cx, cropView.Top);
        yield return new SKPoint(cx, cropView.Bottom);
        yield return new SKPoint(cropView.Left, cy);
        yield return new SKPoint(cropView.Right, cy);
        yield return new SKPoint(cropView.Left, cropView.Top);
        yield return new SKPoint(cropView.Right, cropView.Top);
        yield return new SKPoint(cropView.Left, cropView.Bottom);
        yield return new SKPoint(cropView.Right, cropView.Bottom);
    }

    private void MoveCropHandle(
        CroppingRectangle cropping,
        CropDragHandle handle,
        SKPoint point,
        SKSize moveSize)
    {
        switch (handle)
        {
            case CropDragHandle.TopLeft:
                cropping.MoveCorner(0, point);
                return;
            case CropDragHandle.TopRight:
                cropping.MoveCorner(1, point);
                return;
            case CropDragHandle.BottomRight:
                cropping.MoveCorner(2, point);
                return;
            case CropDragHandle.BottomLeft:
                cropping.MoveCorner(3, point);
                return;
        }

        var rect = cropping.Rect;
        var max = cropping.MaxBounds;
        var minCrop = EditorOptions.Canvas.Crop.MinCropSizeImagePx;

        switch (handle)
        {
            case CropDragHandle.Move:
                var width = moveSize.Width > 0 ? moveSize.Width : rect.Width;
                var height = moveSize.Height > 0 ? moveSize.Height : rect.Height;
                rect.Left = Math.Clamp(point.X, max.Left, max.Right - width);
                rect.Top = Math.Clamp(point.Y, max.Top, max.Bottom - height);
                rect.Right = rect.Left + width;
                rect.Bottom = rect.Top + height;
                break;
            case CropDragHandle.Top:
                rect.Top = Math.Min(Math.Max(point.Y, max.Top), rect.Bottom - minCrop);
                break;
            case CropDragHandle.Bottom:
                rect.Bottom = Math.Max(Math.Min(point.Y, max.Bottom), rect.Top + minCrop);
                break;
            case CropDragHandle.Left:
                rect.Left = Math.Min(Math.Max(point.X, max.Left), rect.Right - minCrop);
                break;
            case CropDragHandle.Right:
                rect.Right = Math.Max(Math.Min(point.X, max.Right), rect.Left + minCrop);
                break;
        }

        cropping.Rect = rect;
    }

    private static SKPoint GetCropHandlePoint(SKRect rect, CropDragHandle handle)
    {
        var cx = rect.MidX;
        var cy = rect.MidY;
        return handle switch
        {
            CropDragHandle.TopLeft => new SKPoint(rect.Left, rect.Top),
            CropDragHandle.TopRight => new SKPoint(rect.Right, rect.Top),
            CropDragHandle.BottomLeft => new SKPoint(rect.Left, rect.Bottom),
            CropDragHandle.BottomRight => new SKPoint(rect.Right, rect.Bottom),
            CropDragHandle.Top => new SKPoint(cx, rect.Top),
            CropDragHandle.Bottom => new SKPoint(cx, rect.Bottom),
            CropDragHandle.Left => new SKPoint(rect.Left, cy),
            CropDragHandle.Right => new SKPoint(rect.Right, cy),
            CropDragHandle.Move => new SKPoint(rect.Left, rect.Top),
            _ => new SKPoint(rect.Left, rect.Top)
        };
    }
}

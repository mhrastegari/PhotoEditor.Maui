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
        var hitRadiusBitmap = Math.Abs(_inverseBitmapMatrix.ScaleX) * EditorOptions.Canvas.Crop.HandleHitRadiusViewPx;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _activeHandle = HitTestCropHandle(bitmapLocation, hitRadiusBitmap);
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

    private CropDragHandle HitTestCropHandle(SKPoint bitmapPoint, float radiusBitmap)
    {
        if (_croppingRect is null)
            return CropDragHandle.None;

        var corner = _croppingRect.HitTestCorner(bitmapPoint, radiusBitmap);
        if (corner >= 0)
        {
            return corner switch
            {
                0 => CropDragHandle.TopLeft,
                1 => CropDragHandle.TopRight,
                2 => CropDragHandle.BottomRight,
                3 => CropDragHandle.BottomLeft,
                _ => CropDragHandle.None
            };
        }

        var rect = _croppingRect.Rect;
        foreach (var (handle, point) in GetCropEdgeHandles(rect))
        {
            var diff = bitmapPoint - point;
            if (diff.LengthSquared < radiusBitmap * radiusBitmap)
                return handle;
        }

        return _croppingRect.Contains(bitmapPoint) ? CropDragHandle.Move : CropDragHandle.None;
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

    private static IEnumerable<(CropDragHandle Handle, SKPoint Point)> GetCropEdgeHandles(SKRect rect)
    {
        var cx = rect.MidX;
        var cy = rect.MidY;
        yield return (CropDragHandle.Top, new SKPoint(cx, rect.Top));
        yield return (CropDragHandle.Bottom, new SKPoint(cx, rect.Bottom));
        yield return (CropDragHandle.Left, new SKPoint(rect.Left, cy));
        yield return (CropDragHandle.Right, new SKPoint(rect.Right, cy));
    }

    private static IEnumerable<SKPoint> GetCropHandlePoints(SKRect cropView)
    {
        foreach (var (_, point) in GetCropEdgeHandles(cropView))
            yield return point;

        yield return new SKPoint(cropView.Left, cropView.Top);
        yield return new SKPoint(cropView.Right, cropView.Top);
        yield return new SKPoint(cropView.Left, cropView.Bottom);
        yield return new SKPoint(cropView.Right, cropView.Bottom);
    }
}

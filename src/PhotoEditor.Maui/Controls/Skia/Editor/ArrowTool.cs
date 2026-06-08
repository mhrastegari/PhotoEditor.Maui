using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void HandleArrowTouch(SKTouchEventArgs e, SKPoint imagePoint)
    {
        var imageBounds = GetImageBounds();
        if (imageBounds.IsEmpty)
            return;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!imageBounds.Contains(imagePoint.X, imagePoint.Y))
                    return;

                _redoArrows.Clear();
                ClearEditRedoStack();
                _activeArrow = new PhotoEditorArrowAnnotation(StrokeColor.ToSkColor(), ViewSizeToBitmapSize(ArrowStrokeWidth))
                {
                    HeadRevealProgress = 0f
                };
                _activeArrow.Points.Add(ClampPointToImageBounds(imagePoint, imageBounds));
                _arrows.Add(_activeArrow);
                CanvasView.InvalidateSurface();
                RaiseDrawingHistoryChanged();
                break;

            case SKTouchAction.Moved:
                if (_activeArrow is null)
                    break;

                _activeArrow.Points.Add(ClampPointToImageBounds(imagePoint, imageBounds));
                CanvasView.InvalidateSurface();
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_activeArrow is null)
                    break;

                _activeArrow.Points.Add(ClampPointToImageBounds(imagePoint, imageBounds));
                var committed = _activeArrow;
                _activeArrow = null;

                if (committed.Points.Count < 2
                    || GetPolylineLength(committed.Points) < ViewSizeToBitmapSize(EditorOptions.Canvas.Arrow.MinLengthViewPx))
                {
                    _arrows.Remove(committed);
                }
                else
                {
                    committed.HeadRevealProgress = 0f;
                    StartArrowHeadRevealAnimation(committed);
                }

                CanvasView.InvalidateSurface();
                RaiseDrawingHistoryChanged();
                break;
        }
    }

    private void StartArrowHeadRevealAnimation(PhotoEditorArrowAnnotation arrow)
    {
        CancelArrowHeadRevealAnimation();
        _arrowHeadRevealTarget = arrow;
        arrow.HeadRevealProgress = 0f;
        _arrowHeadRevealCts = new CancellationTokenSource();
        _ = AnimateArrowHeadRevealAsync(arrow, _arrowHeadRevealCts.Token);
    }

    private void CancelArrowHeadRevealAnimation()
    {
        if (_arrowHeadRevealCts is null)
            return;

        _arrowHeadRevealCts.Cancel();
        _arrowHeadRevealCts.Dispose();
        _arrowHeadRevealCts = null;

        if (_arrowHeadRevealTarget is not null)
        {
            _arrowHeadRevealTarget.HeadRevealProgress = 1f;
            _arrowHeadRevealTarget = null;
        }
    }

    private async Task AnimateArrowHeadRevealAsync(PhotoEditorArrowAnnotation arrow, CancellationToken cancellationToken)
    {
        const int frameCount = 12;
        var frameDelayMs = Math.Max(1, (int)(EditorOptions.Canvas.Arrow.HeadRevealDurationMs / frameCount));

        try
        {
            for (var frame = 1; frame <= frameCount; frame++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_arrows.Contains(arrow))
                    return;

                var progress = frame / (float)frameCount;
                arrow.HeadRevealProgress = progress * progress * (3f - 2f * progress);

                await MainThread.InvokeOnMainThreadAsync(() => CanvasView.InvalidateSurface())
                    .ConfigureAwait(true);
                await Task.Delay(frameDelayMs, cancellationToken).ConfigureAwait(true);
            }

            arrow.HeadRevealProgress = 1f;
            if (ReferenceEquals(_arrowHeadRevealTarget, arrow))
                _arrowHeadRevealTarget = null;

            await MainThread.InvokeOnMainThreadAsync(() => CanvasView.InvalidateSurface())
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer arrow animation or view unload.
        }
        catch (Exception)
        {
            arrow.HeadRevealProgress = 1f;
            if (ReferenceEquals(_arrowHeadRevealTarget, arrow))
                _arrowHeadRevealTarget = null;
        }
    }

    private static float GetPolylineLength(IReadOnlyList<SKPoint> points)
    {
        if (points.Count < 2)
            return 0;

        var length = 0f;
        for (var i = 1; i < points.Count; i++)
        {
            var dx = points[i].X - points[i - 1].X;
            var dy = points[i].Y - points[i - 1].Y;
            length += MathF.Sqrt(dx * dx + dy * dy);
        }

        return length;
    }

    private void DrawArrows(SKCanvas canvas)
    {
        if (_imageDestView.Width <= 0.1f || _imageDestView.Height <= 0.1f)
            return;

        canvas.Save();
        canvas.ClipRect(_imageDestView);

        foreach (var arrow in _arrows)
        {
            if (arrow.Points.Count < 2)
                continue;

            var viewPoints = new SKPoint[arrow.Points.Count];
            for (var i = 0; i < arrow.Points.Count; i++)
                viewPoints[i] = BitmapToView(arrow.Points[i]);

            SkiaPhotoEditorArrowRenderer.DrawPathWithArrowHead(
                canvas,
                viewPoints,
                arrow.Color,
                BitmapSizeToViewSize(arrow.Width),
                arrow.HeadRevealProgress);
        }

        canvas.Restore();
    }
}
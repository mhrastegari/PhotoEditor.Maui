using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void HandleTextTouch(SKTouchEventArgs e, SKPoint imagePoint)
    {
        if (e.ActionType != SKTouchAction.Pressed)
            return;

        var imageBounds = GetImageBounds();
        if (imageBounds.IsEmpty || !imageBounds.Contains(imagePoint.X, imagePoint.Y))
            return;

        _suppressTextInputUnfocusedCommit = true;
        try
        {
            CommitPendingTextOverlayIfNeeded();
            ShowTextInputAt(ClampPointToImageBounds(imagePoint, imageBounds));
        }
        finally
        {
            Dispatcher.Dispatch(async () =>
            {
                TextInputEntry.Focus();
                await Task.Delay(100).ConfigureAwait(true);
                _suppressTextInputUnfocusedCommit = false;
            });
        }
    }

    private void ShowTextInputAt(SKPoint bitmapCenter)
    {
        _pendingTextBitmapCenter = bitmapCenter;
        TextInputEntry.Text = string.Empty;
        ApplyTextInputEntryAppearance();
        TextInputHost.IsVisible = true;
        LayoutTextInputHostAtAnchor();
        CanvasView.InvalidateSurface();
    }

    private void ApplyTextInputEntryAppearance()
    {
        TextInputEntry.TextColor = Colors.Transparent;
        TextInputEntry.Placeholder = string.Empty;
        TextInputEntry.FontAttributes = FontAttributes.None;
        TextInputEntry.FontSize = GetTextFontSizeInLayoutDip();
        var entryHeight = GetTextInputEntryHeightDip();
        TextInputEntry.HeightRequest = entryHeight;
        TextInputEntry.MinimumHeightRequest = entryHeight;
    }

    private string GetTextInputPreviewText() => TextInputEntry.Text ?? string.Empty;

    private double GetTextInputEntryHeightDip() =>
        GetTextFontSizeInLayoutDip() * 1.35 + EditorOptions.Canvas.TextInput.EntryLayoutPaddingDip;

    private void RefreshVisibleTextInputHost()
    {
        if (!TextInputHost.IsVisible)
            return;

        ApplyTextInputEntryAppearance();
        LayoutTextInputHostAtAnchor();
        CanvasView.InvalidateSurface();
    }

    private double GetTextFontSizeInLayoutDip() =>
        Math.Max(8, TextFontSize * GetCanvasToLayoutScaleX());

    private float GetTextFontSizeInViewPixels() => TextFontSize;

    private float GetTextFontSizeForBitmap() => ViewSizeToBitmapSize(TextFontSize);

    private void LayoutTextInputHostAtAnchor()
    {
        if (!TextInputHost.IsVisible)
            return;

        var (hostWidth, hostHeight) = MeasureTextInputHost();
        var layoutCenter = BitmapCenterToLayoutPoint(_pendingTextBitmapCenter);
        var x = layoutCenter.X - hostWidth / 2;
        var y = layoutCenter.Y - hostHeight / 2;

        AbsoluteLayout.SetLayoutFlags(TextInputHost, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(TextInputHost, new Rect(x, y, hostWidth, hostHeight));
    }

    private (double Width, double Height) MeasureTextInputHost()
    {
        var previewText = GetTextInputPreviewText();
        var viewFontSize = GetTextFontSizeInViewPixels();
        var (textWidthPx, _) = SkiaPhotoEditorTextRenderer.Measure(previewText, viewFontSize);
        var textWidth = Math.Max(textWidthPx * GetCanvasToLayoutScaleX(), TextInputEntry.MinimumWidthRequest);
        var entryHeight = GetTextInputEntryHeightDip();

        const double dragHandleHeight = 6;
        const double dragHandleSpacing = 4;
        var width = textWidth + TextInputHost.Padding.HorizontalThickness;
        var height = entryHeight
                     + dragHandleHeight
                     + dragHandleSpacing
                     + TextInputHost.Padding.VerticalThickness;

        return (
            Math.Max(width, TextInputEntry.MinimumWidthRequest + TextInputHost.Padding.HorizontalThickness),
            Math.Max(height, entryHeight + dragHandleHeight + dragHandleSpacing + 8));
    }

    private Point BitmapCenterToLayoutPoint(SKPoint bitmapCenter)
    {
        var canvasPoint = BitmapToView(bitmapCenter);
        return CanvasPixelToLayoutPoint(canvasPoint);
    }

    private SKPoint GetTextInputHostBitmapCenter()
    {
        var bounds = AbsoluteLayout.GetLayoutBounds(TextInputHost);
        var (hostWidth, hostHeight) = GetTextInputHostLayoutSize(bounds);
        var layoutCenter = new Point(bounds.X + hostWidth / 2, bounds.Y + hostHeight / 2);
        var canvasCenter = LayoutPointToCanvasPixel(layoutCenter);
        return ViewToBitmap(canvasCenter);
    }

    private SKPoint GetPendingTextPreviewCenter()
    {
        if (!TextInputHost.IsVisible)
            return _pendingTextBitmapCenter;

        var bounds = AbsoluteLayout.GetLayoutBounds(TextInputHost);
        if (bounds.Width <= 0 && bounds.Height <= 0)
            return _pendingTextBitmapCenter;

        return GetTextInputHostBitmapCenter();
    }

    private (double Width, double Height) GetTextInputHostLayoutSize(Rect bounds)
    {
        var width = bounds.Width > 0 ? bounds.Width : TextInputHost.Width;
        var height = bounds.Height > 0 ? bounds.Height : TextInputHost.Height;
        if (width <= 0)
            width = MeasureTextInputHost().Width;
        if (height <= 0)
            height = MeasureTextInputHost().Height;
        return (width, height);
    }

    private void SyncPendingTextCenterFromHost()
    {
        _pendingTextBitmapCenter = ClampPointToImageBounds(
            GetTextInputHostBitmapCenter(),
            GetImageBounds());
    }

    private void OnTextInputEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!TextInputHost.IsVisible || _isTextInputDragging)
            return;

        LayoutTextInputHostAtAnchor();
        CanvasView.InvalidateSurface();
    }

    private void OnTextInputHostSizeChanged(object? sender, EventArgs e)
    {
        if (TextInputHost.IsVisible && !_isTextInputDragging)
            LayoutTextInputHostAtAnchor();
    }

    private void OnTextInputPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!TextInputHost.IsVisible)
            return;

        var bounds = AbsoluteLayout.GetLayoutBounds(TextInputHost);
        var (hostWidth, hostHeight) = GetTextInputHostLayoutSize(bounds);

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isTextInputDragging = true;
                _textInputPanLayoutOrigin = new Point(bounds.X, bounds.Y);
                TextInputEntry.Unfocus();
                break;

            case GestureStatus.Running:
                AbsoluteLayout.SetLayoutBounds(
                    TextInputHost,
                    new Rect(
                        _textInputPanLayoutOrigin.X + e.TotalX,
                        _textInputPanLayoutOrigin.Y + e.TotalY,
                        hostWidth,
                        hostHeight));
                CanvasView.InvalidateSurface();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isTextInputDragging = false;
                SyncPendingTextCenterFromHost();
                CanvasView.InvalidateSurface();
                break;
        }
    }

    private void HideTextInputHost()
    {
        if (!TextInputHost.IsVisible)
            return;

        TextInputHost.IsVisible = false;
        TextInputEntry.Text = string.Empty;
        TextInputEntry.Unfocus();
        PhotoEditorKeyboardHelper.HideKeyboardAndClearFocus(TextInputEntry);
        CanvasView.InvalidateSurface();
    }

    private void OnTextInputCompleted(object? sender, EventArgs e) => CommitPendingTextInput();

    private void OnTextInputUnfocused(object? sender, FocusEventArgs e)
    {
        if (_isTextInputDragging)
            return;

        Dispatcher.Dispatch(() =>
        {
            if (_isTextInputDragging || _suppressTextInputUnfocusedCommit)
                return;

            if (InteractionMode == SkiaPhotoEditorInteractionMode.Text && TextInputHost.IsVisible)
                return;

            CommitPendingTextInput();
        });
    }

    private void CommitPendingTextOverlayIfNeeded()
    {
        if (!TextInputHost.IsVisible || _isCommittingTextInput)
            return;

        _isCommittingTextInput = true;
        try
        {
            var text = TextInputEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                SyncPendingTextCenterFromHost();
                _textRedoOverlays.Clear();
                ClearEditRedoStack();
                _textOverlays.Add(new PhotoEditorTextOverlay(
                    text,
                    _pendingTextBitmapCenter,
                    StrokeColor.ToSkColor(),
                    GetTextFontSizeForBitmap()));
                RaiseDrawingHistoryChanged();
            }

            TextInputEntry.Text = string.Empty;
        }
        finally
        {
            _isCommittingTextInput = false;
            CanvasView.InvalidateSurface();
        }
    }

    private void CommitPendingTextInput()
    {
        CommitPendingTextOverlayIfNeeded();
        HideTextInputHost();
    }

    private void DrawTextOverlays(SKCanvas canvas)
    {
        if (_imageDestView.Width <= 0.1f || _imageDestView.Height <= 0.1f)
            return;

        canvas.Save();
        canvas.ClipRect(_imageDestView);

        foreach (var overlay in _textOverlays)
        {
            if (string.IsNullOrWhiteSpace(overlay.Text))
                continue;

            var viewCenter = BitmapToView(overlay.Position);
            SkiaPhotoEditorTextRenderer.DrawAtCenter(
                canvas,
                overlay.Text,
                viewCenter.X,
                viewCenter.Y,
                BitmapSizeToViewSize(overlay.FontSize),
                overlay.Color);
        }

        canvas.Restore();
    }

    private void DrawPendingTextPreview(SKCanvas canvas)
    {
        if (!TextInputHost.IsVisible || InteractionMode != SkiaPhotoEditorInteractionMode.Text)
            return;

        var text = GetTextInputPreviewText();
        if (string.IsNullOrEmpty(text))
            return;

        if (_imageDestView.Width <= 0.1f || _imageDestView.Height <= 0.1f)
            return;

        canvas.Save();
        canvas.ClipRect(_imageDestView);

        var viewCenter = BitmapToView(GetPendingTextPreviewCenter());
        var fontSize = GetTextFontSizeInViewPixels();
        SkiaPhotoEditorTextRenderer.DrawAtCenter(
            canvas,
            text,
            viewCenter.X,
            viewCenter.Y,
            fontSize,
            StrokeColor.ToSkColor());

        canvas.Restore();
    }
}
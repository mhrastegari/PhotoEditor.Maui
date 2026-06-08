using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    public void StartCropMode()
    {
        if (!EditorOptions.Features.Crop || !IsImageLoaded) return;

        InteractionMode = SkiaPhotoEditorInteractionMode.Crop;
        _activeStroke = null;
        _activeHandle = CropDragHandle.None;
        _croppingRectNeedsSync = true;

        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void CancelCropMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Crop)
            return;

        InteractionMode = SkiaPhotoEditorInteractionMode.None;
        _activeHandle = CropDragHandle.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public Task ApplyCropAsync()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Crop || !IsImageLoaded)
            return Task.CompletedTask;

        return RunOnUiAsync(ApplyCropAsyncCore);
    }

    public Task RotateCropClockwiseAsync()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Crop || !IsImageLoaded)
            return Task.CompletedTask;

        return RunOnUiAsync(RotateCropClockwiseAsyncCore);
    }

    public void StartDrawMode()
    {
        if (!EditorOptions.Features.Draw || !IsImageLoaded)
            return;

        BeginOverlaySession(() => _drawSessionStrokeCount = _strokes.Count);
        InteractionMode = SkiaPhotoEditorInteractionMode.Draw;
        _activeHandle = CropDragHandle.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void StopDrawMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Draw)
            return;

        _activeStroke = null;
        InteractionMode = SkiaPhotoEditorInteractionMode.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void ApplyDrawMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Draw)
            return;

        BakeDrawingsIntoImage();
        StopDrawMode();
    }

    public void CancelDrawMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Draw)
            return;

        while (_strokes.Count > _drawSessionStrokeCount)
            _strokes.RemoveAt(_strokes.Count - 1);

        _redoStrokes.Clear();
        _activeStroke = null;
        DiscardDrawSessionHistoryEntry();
        StopDrawMode();
        CanvasView.InvalidateSurface();
        RaiseDrawingHistoryChanged();
    }

    public void StartArrowMode()
    {
        if (!EditorOptions.Features.Arrow || !IsImageLoaded)
            return;

        BeginOverlaySession(() => _arrowSessionArrowCount = _arrows.Count);
        InteractionMode = SkiaPhotoEditorInteractionMode.Arrow;
        _activeHandle = CropDragHandle.None;
        _activeStroke = null;
        _activeArrow = null;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void StopArrowMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Arrow)
            return;

        _activeArrow = null;
        InteractionMode = SkiaPhotoEditorInteractionMode.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void ApplyArrowMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Arrow)
            return;

        BakeDrawingsIntoImage();
        StopArrowMode();
    }

    public void CancelArrowMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Arrow)
            return;

        while (_arrows.Count > _arrowSessionArrowCount)
            _arrows.RemoveAt(_arrows.Count - 1);

        _redoArrows.Clear();
        _activeArrow = null;
        DiscardDrawSessionHistoryEntry();
        StopArrowMode();
        CanvasView.InvalidateSurface();
        RaiseDrawingHistoryChanged();
    }

    public void StartTextMode()
    {
        if (!EditorOptions.Features.Text || !IsImageLoaded)
            return;

        BeginOverlaySession(() => _textSessionOverlayCount = _textOverlays.Count);
        InteractionMode = SkiaPhotoEditorInteractionMode.Text;
        _activeHandle = CropDragHandle.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void StopTextMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Text)
            return;

        HideTextInputHost();
        InteractionMode = SkiaPhotoEditorInteractionMode.None;
        RaiseInteractionModeChanged();
        CanvasView.InvalidateSurface();
    }

    public void RestoreTextInputFocus()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Text || !TextInputHost.IsVisible)
            return;

        Dispatcher.Dispatch(() => TextInputEntry.Focus());
    }

    public void ApplyTextMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Text)
            return;

        CommitPendingTextInput();
        BakeTextsIntoImage();
        StopTextMode();
    }

    public void CancelTextMode()
    {
        if (InteractionMode != SkiaPhotoEditorInteractionMode.Text)
            return;

        HideTextInputHost();

        while (_textOverlays.Count > _textSessionOverlayCount)
            _textOverlays.RemoveAt(_textOverlays.Count - 1);

        _textRedoOverlays.Clear();
        DiscardDrawSessionHistoryEntry();
        StopTextMode();
        CanvasView.InvalidateSurface();
        RaiseDrawingHistoryChanged();
    }

    public void UndoLastStroke()
    {
        if (InteractionMode == SkiaPhotoEditorInteractionMode.Draw && _strokes.Count > 0)
        {
            var last = _strokes[^1];
            _strokes.RemoveAt(_strokes.Count - 1);
            _redoStrokes.Add(last);
            _activeStroke = null;
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Arrow && _arrows.Count > _arrowSessionArrowCount)
        {
            var last = _arrows[^1];
            _arrows.RemoveAt(_arrows.Count - 1);
            _redoArrows.Add(last);
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Text && _textOverlays.Count > _textSessionOverlayCount)
        {
            var last = _textOverlays[^1];
            _textOverlays.RemoveAt(_textOverlays.Count - 1);
            _textRedoOverlays.Add(last);
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode != SkiaPhotoEditorInteractionMode.None)
            return;

        UndoEdit();
    }

    public void CancelActiveTool()
    {
        switch (InteractionMode)
        {
            case SkiaPhotoEditorInteractionMode.Draw:
                CancelDrawMode();
                break;
            case SkiaPhotoEditorInteractionMode.Arrow:
                CancelArrowMode();
                break;
            case SkiaPhotoEditorInteractionMode.Text:
                CancelTextMode();
                break;
            default:
                CancelCropMode();
                break;
        }
    }

    public Task ApplyActiveToolAsync()
    {
        switch (InteractionMode)
        {
            case SkiaPhotoEditorInteractionMode.Draw:
                ApplyDrawMode();
                return Task.CompletedTask;
            case SkiaPhotoEditorInteractionMode.Arrow:
                ApplyArrowMode();
                return Task.CompletedTask;
            case SkiaPhotoEditorInteractionMode.Text:
                ApplyTextMode();
                return Task.CompletedTask;
            case SkiaPhotoEditorInteractionMode.Crop:
                return ApplyCropAsync();
            default:
                return Task.CompletedTask;
        }
    }

    public void ToggleTool(SkiaPhotoEditorInteractionMode mode)
    {
        if (InteractionMode == mode)
            CancelActiveTool();
        else
            ActivateTool(mode);
    }

    public void ActivateTool(SkiaPhotoEditorInteractionMode mode)
    {
        CancelCropMode();
        CancelDrawMode();
        CancelArrowMode();
        CancelTextMode();

        switch (mode)
        {
            case SkiaPhotoEditorInteractionMode.Crop:
                StartCropMode();
                break;
            case SkiaPhotoEditorInteractionMode.Draw:
                StartDrawMode();
                break;
            case SkiaPhotoEditorInteractionMode.Arrow:
                StartArrowMode();
                break;
            case SkiaPhotoEditorInteractionMode.Text:
                StartTextMode();
                break;
        }
    }

    public void RedoLastStroke()
    {
        if (InteractionMode == SkiaPhotoEditorInteractionMode.Draw && _redoStrokes.Count > 0)
        {
            var stroke = _redoStrokes[^1];
            _redoStrokes.RemoveAt(_redoStrokes.Count - 1);
            _strokes.Add(stroke);
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Arrow && _redoArrows.Count > 0)
        {
            var arrow = _redoArrows[^1];
            _redoArrows.RemoveAt(_redoArrows.Count - 1);
            _arrows.Add(arrow);
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode == SkiaPhotoEditorInteractionMode.Text && _textRedoOverlays.Count > 0)
        {
            var overlay = _textRedoOverlays[^1];
            _textRedoOverlays.RemoveAt(_textRedoOverlays.Count - 1);
            _textOverlays.Add(overlay);
            CanvasView.InvalidateSurface();
            RaiseDrawingHistoryChanged();
            return;
        }

        if (InteractionMode != SkiaPhotoEditorInteractionMode.None)
            return;

        RedoEdit();
    }

    private void BeginOverlaySession(Action captureBaseline)
    {
        captureBaseline();
        PushEditHistory();
        _overlaySessionHistoryIndex = _editUndoStack.Count - 1;
    }
}
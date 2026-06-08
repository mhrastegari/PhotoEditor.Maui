using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void PushEditHistory()
    {
        var snapshot = CaptureCurrentState();
        if (snapshot is null)
            return;

        ClearEditRedoStack();
        _editUndoStack.Add(snapshot);

        while (_editUndoStack.Count > EditorOptions.Canvas.MaxUndoHistoryDepth)
        {
            _editUndoStack[0].Dispose();
            _editUndoStack.RemoveAt(0);
            if (_overlaySessionHistoryIndex >= 0)
                _overlaySessionHistoryIndex--;
        }

        RaiseDrawingHistoryChanged();
    }

    private void DiscardDrawSessionHistoryEntry()
    {
        if (_overlaySessionHistoryIndex < 0 || _overlaySessionHistoryIndex >= _editUndoStack.Count)
        {
            _overlaySessionHistoryIndex = -1;
            return;
        }

        _editUndoStack[_overlaySessionHistoryIndex].Dispose();
        _editUndoStack.RemoveAt(_overlaySessionHistoryIndex);
        _overlaySessionHistoryIndex = -1;
    }

    private void UndoEdit()
    {
        if (_editUndoStack.Count == 0)
            return;

        var current = CaptureCurrentState();
        if (current is not null)
            _editRedoStack.Add(current);

        var removedIndex = _editUndoStack.Count - 1;
        var previous = _editUndoStack[removedIndex];
        _editUndoStack.RemoveAt(removedIndex);
        AdjustOverlaySessionHistoryIndexAfterRemoval(removedIndex);
        RestoreSnapshot(previous);
        previous.Dispose();
    }

    private void RedoEdit()
    {
        if (_editRedoStack.Count == 0)
            return;

        var current = CaptureCurrentState();
        if (current is not null)
            _editUndoStack.Add(current);

        var next = _editRedoStack[^1];
        _editRedoStack.RemoveAt(_editRedoStack.Count - 1);
        RestoreSnapshot(next);
        next.Dispose();
    }

    private EditSnapshot? CaptureCurrentState()
    {
        SKBitmap? copy;
        lock (_bitmapLock)
        {
            if (_bitmap is null || _bitmap.IsNull)
                return null;

            copy = _bitmap.Copy();
        }

        if (copy is null || copy.IsNull)
            return null;

        return new EditSnapshot(copy, CloneStrokes(_strokes), CloneArrows(_arrows), CloneTextOverlays(_textOverlays));
    }

    private void RestoreSnapshot(EditSnapshot snapshot)
    {
        lock (_bitmapLock)
        {
            _bitmap?.Dispose();
            _bitmap = snapshot.Bitmap.Copy();
        }

        _strokes.Clear();
        foreach (var stroke in CloneStrokes(snapshot.Strokes))
            _strokes.Add(stroke);

        _arrows.Clear();
        foreach (var arrow in CloneArrows(snapshot.Arrows))
            _arrows.Add(arrow);

        _textOverlays.Clear();
        foreach (var overlay in CloneTextOverlays(snapshot.TextOverlays))
            _textOverlays.Add(overlay);

        _redoStrokes.Clear();
        _redoArrows.Clear();
        _textRedoOverlays.Clear();
        HideTextInputHost();
        _activeStroke = null;
        _activeArrow = null;
        _activeHandle = CropDragHandle.None;
        _croppingRectNeedsSync = true;
        InteractionMode = SkiaPhotoEditorInteractionMode.None;
        CanvasView.InvalidateSurface();
        RaiseInteractionModeChanged();
        RaiseDrawingHistoryChanged();
    }

    private static StrokePath CloneStroke(StrokePath source)
    {
        var clone = new StrokePath(source.Color, source.Width);
        clone.Points.AddRange(source.Points);
        return clone;
    }

    private static List<StrokePath> CloneStrokes(IEnumerable<StrokePath> source)
    {
        var list = new List<StrokePath>();
        foreach (var stroke in source)
            list.Add(CloneStroke(stroke));

        return list;
    }

    private static PhotoEditorArrowAnnotation CloneArrow(PhotoEditorArrowAnnotation source)
    {
        var clone = new PhotoEditorArrowAnnotation(source.Color, source.Width)
        {
            HeadRevealProgress = 1f
        };
        clone.Points.AddRange(source.Points);
        return clone;
    }

    private static List<PhotoEditorArrowAnnotation> CloneArrows(IEnumerable<PhotoEditorArrowAnnotation> source)
    {
        var list = new List<PhotoEditorArrowAnnotation>();
        foreach (var arrow in source)
            list.Add(CloneArrow(arrow));

        return list;
    }

    private static PhotoEditorTextOverlay CloneTextOverlay(PhotoEditorTextOverlay source) =>
        new(source.Text, source.Position, source.Color, source.FontSize);

    private static List<PhotoEditorTextOverlay> CloneTextOverlays(IEnumerable<PhotoEditorTextOverlay> source)
    {
        var list = new List<PhotoEditorTextOverlay>();
        foreach (var overlay in source)
            list.Add(CloneTextOverlay(overlay));

        return list;
    }

    private void ClearEditRedoStack()
    {
        foreach (var snapshot in _editRedoStack)
            snapshot.Dispose();

        _editRedoStack.Clear();
    }

    private void AdjustOverlaySessionHistoryIndexAfterRemoval(int removedIndex)
    {
        if (_overlaySessionHistoryIndex == removedIndex)
            _overlaySessionHistoryIndex = -1;
        else if (_overlaySessionHistoryIndex > removedIndex)
            _overlaySessionHistoryIndex--;
    }

    private void ClearEditHistoryStacks()
    {
        foreach (var snapshot in _editUndoStack)
            snapshot.Dispose();

        _editUndoStack.Clear();
        _overlaySessionHistoryIndex = -1;
        ClearEditRedoStack();
    }
}
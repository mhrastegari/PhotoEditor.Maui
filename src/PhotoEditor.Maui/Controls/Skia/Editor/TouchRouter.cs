using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    private void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        if (IsLoading || !IsImageLoaded)
        {
            e.Handled = true;
            return;
        }

        EnsureLayoutForTouch();

        switch (InteractionMode)
        {
            case SkiaPhotoEditorInteractionMode.Crop:
                if (_surfaceWidth <= 0 || _croppingRect is null)
                    break;
                HandleCropTouch(e);
                break;
            case SkiaPhotoEditorInteractionMode.Draw:
                HandleDrawTouch(e, ViewToBitmap(GetCanvasTouchPixel(e)));
                break;
            case SkiaPhotoEditorInteractionMode.Arrow:
                HandleArrowTouch(e, ViewToBitmap(GetCanvasTouchPixel(e)));
                break;
            case SkiaPhotoEditorInteractionMode.Text:
                HandleTextTouch(e, ViewToBitmap(GetCanvasTouchPixel(e)));
                break;
        }

        e.Handled = true;
    }
}
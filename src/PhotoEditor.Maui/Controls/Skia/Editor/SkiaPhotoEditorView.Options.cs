namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options),
        typeof(PhotoEditorOptions),
        typeof(SkiaPhotoEditorView),
        defaultValueCreator: _ => PhotoEditorOptions.Default,
        propertyChanged: OnOptionsChanged);

    /// <summary>Effective options for this editor instance.</summary>
    public PhotoEditorOptions Options
    {
        get => (PhotoEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    internal PhotoEditorOptions EditorOptions => Options;

    private void SubscribeThemeChanges()
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void UnsubscribeThemeChanges()
    {
        Application.Current?.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyCanvasThemeFromOptions();

    internal void ApplyCanvasThemeFromOptions()
    {
        var resolvedTheme = PhotoEditorResolvedTheme.Resolve(this, EditorOptions.Theme);
        var text = EditorOptions.Canvas.TextInput;

        LoadingOverlay.BackgroundColor = resolvedTheme.LoadingOverlayColor;
        LoadingIndicator.Color = resolvedTheme.LoadingIndicatorColor;
        TextInputHost.Stroke = text.BorderColor;
        TextInputDragHandle.BackgroundColor = text.DragHandleColor;
        TextInputEntry.MinimumWidthRequest = text.EntryMinWidthDip;
    }

    private void ApplyOptionsFromConfiguration()
    {
        var canvas = EditorOptions.Canvas;
        StrokeColor = canvas.DefaultStrokeColor;
        DrawStrokeWidth = canvas.DefaultStrokeWidth;
        ArrowStrokeWidth = canvas.Arrow.DefaultStrokeWidth;
        TextFontSize = canvas.DefaultTextFontSize;
        ApplyCanvasThemeFromOptions();
        RefreshVisibleTextInputHost();
        CanvasView.InvalidateSurface();
    }

    private static void OnOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SkiaPhotoEditorView editor)
            editor.ApplyOptionsFromConfiguration();
    }
}

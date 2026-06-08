namespace PhotoEditor.Maui;

public partial class PhotoEditorView
{
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options),
        typeof(PhotoEditorOptions),
        typeof(PhotoEditorView),
        defaultValueCreator: _ => PhotoEditorOptions.Default,
        propertyChanged: OnOptionsChanged);

    public PhotoEditorOptions Options
    {
        get => (PhotoEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    private PhotoEditorThemeOptions Theme => Options.Theme;

    private static void OnOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhotoEditorView editor)
            return;

        editor.PhotoEditorControl.Options = editor.Options;
        editor.ApplyThemeFromOptions();
        editor.UpdateToolbarForInteractionMode();
        editor.RebuildToolPalettes();
    }

    private void RebuildToolPalettes()
    {
        DrawColorPalette.Children.Clear();
        TextFontSizePalette.Children.Clear();
        if (Handler is null || !PhotoEditorControl.IsImageLoaded)
            return;

        EnsureDrawColorPalette();
        EnsureTextFontSizePalette();
    }
}

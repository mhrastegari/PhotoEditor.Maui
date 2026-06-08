namespace PhotoEditor.Maui.Sample.Themes;

/// <summary>
/// Customized built-in <see cref="PhotoEditorView"/> theme: colors, rounded buttons, and Material Icons toolbar.
/// Register <see cref="IconFontFamily"/> in <c>MauiProgram.cs</c>.
/// </summary>
public static class ThemedDefaultEditorTheme
{
    public const string IconFontFamily = "MaterialIcons";

    public static Color Accent { get; } = Color.FromArgb("#7C3AED");
    public static Color PageBackground { get; } = Color.FromArgb("#F1F5F9");
    public static Color Surface { get; } = Colors.White;
    public static Color Text { get; } = Color.FromArgb("#1E293B");
    public static Color TextMuted { get; } = Color.FromArgb("#64748B");
    public static Color Border { get; } = Color.FromArgb("#E2E8F0");

    public static PhotoEditorOptions CreateOptions() => new()
    {
        Theme = new PhotoEditorThemeOptions
        {
            AccentColor = Accent,
            SurfaceColor = Surface,
            TextPrimaryColor = Text,
            TextSecondaryColor = TextMuted,
            DoneButtonTextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            ToolbarBackgroundColor = Surface,
            PaletteBackgroundColor = Surface,
            SecondaryButtonBackgroundColor = Surface,
            ButtonBorderColor = Border,
            ActiveToolBackgroundColor = Color.FromArgb("#EDE9FE"),
            ActiveToolBorderColor = Accent,
            ButtonCornerRadius = 22,
            ButtonBorderWidth = 1,
            ToolbarCornerRadius = 24,
            LoadingOverlayColor = Color.FromArgb("#CCF1F5F9"),
            LoadingIndicatorColor = Accent,
            ToolModeCanvasMargin = new Thickness(28, 0),
            Toolbar = MaterialIconsToolbar.Create(IconFontFamily),
        },
        Canvas = new PhotoEditorCanvasOptions
        {
            Crop = new PhotoEditorCropOptions
            {
                OverlayBorderColor = Accent,
                OverlayHandleColor = Accent,
            },
            TextInput = new PhotoEditorTextInputOptions
            {
                BorderColor = Accent,
                DragHandleColor = Accent,
            },
        },
    };
}

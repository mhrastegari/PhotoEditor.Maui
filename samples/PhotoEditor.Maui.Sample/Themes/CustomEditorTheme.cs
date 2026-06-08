namespace PhotoEditor.Maui.Sample.Themes;

/// <summary>
/// Brand colors and <see cref="PhotoEditorOptions"/> for the custom UI demo page.
/// </summary>
public static class CustomEditorTheme
{
    public static Color Accent { get; } = Color.FromArgb("#FF6B6B");
    public static Color Background { get; } = Color.FromArgb("#1A1A2E");
    public static Color Surface { get; } = Color.FromArgb("#2D2D44");
    public static Color Text { get; } = Color.FromArgb("#F5F5F5");
    public static Color TextMuted { get; } = Color.FromArgb("#A0A0B8");

    public static PhotoEditorOptions CreateOptions() => new()
    {
        Theme = new PhotoEditorThemeOptions
        {
            AccentColor = Accent,
            SurfaceColor = Surface,
            TextPrimaryColor = Text,
            TextSecondaryColor = TextMuted,
            DoneButtonTextColor = Text,
            BackgroundColor = Background,
            ToolbarBackgroundColor = Surface,
            PaletteBackgroundColor = Color.FromArgb("#252538"),
            SecondaryButtonBackgroundColor = Color.FromArgb("#252538"),
            ButtonBorderColor = Color.FromArgb("#3D3D5C"),
            ActiveToolBackgroundColor = Color.FromArgb("#33FF6B6B"),
            ActiveToolBorderColor = Accent,
            LoadingOverlayColor = Color.FromArgb("#CC1A1A2E"),
            LoadingIndicatorColor = Accent,
            ToolModeCanvasMargin = new Thickness(24, 0),
            ToolModeMarginAnimationDurationMs = 180,
        },
        Canvas = new PhotoEditorCanvasOptions
        {
            DrawColors =
            [
                Color.FromArgb("#FF6B6B"),
                Color.FromArgb("#FF8E53"),
                Color.FromArgb("#FFD93D"),
                Color.FromArgb("#6BCB77"),
                Color.FromArgb("#00C9A7"),
                Color.FromArgb("#4D96FF"),
                Color.FromArgb("#9B5DE5"),
                Color.FromArgb("#F15BB5"),
                Color.FromArgb("#FEE440"),
                Colors.White,
                Color.FromArgb("#B0B0C8"),
                Colors.Black,
            ],
            TextFontSizes = [20f, 24f, 28f, 32f, 40f, 48f, 56f, 64f, 72f],
            DefaultStrokeColor = Color.FromArgb("#FF6B6B"),
            DefaultStrokeWidth = 8f,
            DrawStrokeWidthMin = 2f,
            DrawStrokeWidthMax = 20f,
            DefaultTextFontSize = 40f,
            Arrow = new PhotoEditorArrowOptions
            {
                DefaultStrokeWidth = 10f,
                StrokeWidthMin = 3f,
                StrokeWidthMax = 28f,
            },
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
        Messages = new PhotoEditorMessageOptions
        {
            DiscardTitle = "Leave editor?",
            DiscardMessage = "Unsaved edits will be lost.",
            DiscardConfirm = "Leave",
            DiscardCancel = "Stay",
        },
    };
}

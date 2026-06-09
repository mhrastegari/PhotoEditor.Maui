namespace PhotoEditor.Maui;

/// <summary>Resolved theme colors and metrics after applying app resources and light/dark fallbacks.</summary>
internal readonly struct PhotoEditorResolvedTheme
{
    public Color AccentColor { get; init; }
    public Color SurfaceColor { get; init; }
    public Color TextPrimaryColor { get; init; }
    public Color TextSecondaryColor { get; init; }
    public Color DoneButtonTextColor { get; init; }
    public Color BackgroundColor { get; init; }
    public Color ToolbarBackgroundColor { get; init; }
    public Color PaletteBackgroundColor { get; init; }
    public Color SecondaryButtonBackgroundColor { get; init; }
    public Color ButtonBorderColor { get; init; }
    public Color ActiveToolBackgroundColor { get; init; }
    public Color ActiveToolBorderColor { get; init; }
    public double ButtonCornerRadius { get; init; }
    public double ButtonBorderWidth { get; init; }
    public double ToolbarCornerRadius { get; init; }
    public Color LoadingOverlayColor { get; init; }
    public Color LoadingIndicatorColor { get; init; }
    public Thickness ToolModeCanvasMargin { get; init; }
    public uint ToolModeMarginAnimationDurationMs { get; init; }

    public static PhotoEditorResolvedTheme Resolve(VisualElement? context, PhotoEditorThemeOptions options)
    {
        var appTheme = GetEffectiveAppTheme();
        var accent = ResolveColor(options.AccentColor, context, appTheme, "Primary", "PrimaryDark", 0x512BD4, 0xAC99EA);
        var surface = ResolveColor(options.SurfaceColor, context, appTheme, "Secondary", "Gray950", 0xDFD8F7, 0x141414);
        var textPrimary = ResolveColor(options.TextPrimaryColor, context, appTheme, "Gray900", "White", 0x212121, 0xFFFFFF);
        var textSecondary = ResolveColor(options.TextSecondaryColor, context, appTheme, "Gray500", "Gray300", 0x6E6E6E, 0xACACAC);
        var doneText = ResolveColor(options.DoneButtonTextColor, context, appTheme, "White", "PrimaryDarkText", 0xFFFFFF, 0x242424);
        var toolbar = ResolveColor(options.ToolbarBackgroundColor, context, appTheme, "White", "Gray950", 0xFFFFFF, 0x141414);
        var palette = ResolveColor(options.PaletteBackgroundColor, context, appTheme, "Gray100", "Gray950", 0xE1E1E1, 0x141414);
        var secondaryButton = ResolveColor(options.SecondaryButtonBackgroundColor, context, appTheme, "White", "Gray600", 0xFFFFFF, 0x404040);
        var border = ResolveColor(options.ButtonBorderColor, context, appTheme, "Gray200", "Gray500", 0xC8C8C8, 0x6E6E6E);
        var activeBorder = options.ActiveToolBorderColor ?? accent;
        var activeBackground = options.ActiveToolBackgroundColor ?? activeBorder.WithAlpha(0.12f);
        var loadingSurface = ResolveColor(options.LoadingOverlayColor, context, appTheme, "Gray100", "Gray950", 0xE1E1E1, 0x141414);
        var loadingOverlay = options.LoadingOverlayColor ?? loadingSurface.WithAlpha(0.8f);
        var loadingIndicator = options.LoadingIndicatorColor ?? accent;

        return new PhotoEditorResolvedTheme
        {
            AccentColor = accent,
            SurfaceColor = surface,
            TextPrimaryColor = textPrimary,
            TextSecondaryColor = textSecondary,
            DoneButtonTextColor = doneText,
            BackgroundColor = options.BackgroundColor ?? Colors.Transparent,
            ToolbarBackgroundColor = toolbar,
            PaletteBackgroundColor = palette,
            SecondaryButtonBackgroundColor = secondaryButton,
            ButtonBorderColor = border,
            ActiveToolBackgroundColor = activeBackground,
            ActiveToolBorderColor = activeBorder,
            ButtonCornerRadius = options.ButtonCornerRadius ?? 8,
            ButtonBorderWidth = options.ButtonBorderWidth ?? 1,
            ToolbarCornerRadius = options.ToolbarCornerRadius ?? 8,
            LoadingOverlayColor = loadingOverlay,
            LoadingIndicatorColor = loadingIndicator,
            ToolModeCanvasMargin = options.ToolModeCanvasMargin ?? new Thickness(32, 0),
            ToolModeMarginAnimationDurationMs = options.ToolModeMarginAnimationDurationMs ?? 220,
        };
    }

    private static AppTheme GetEffectiveAppTheme()
    {
        var theme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
        if (theme == AppTheme.Unspecified)
            theme = Application.Current?.PlatformAppTheme ?? AppTheme.Light;
        return theme;
    }

    private static Color ResolveColor(
        Color? value,
        VisualElement? context,
        AppTheme theme,
        string lightResourceKey,
        string darkResourceKey,
        uint lightArgb,
        uint darkArgb)
    {
        if (value is not null)
            return value;

        var key = theme == AppTheme.Dark ? darkResourceKey : lightResourceKey;
        if (TryGetResourceColor(context, key, out var resourceColor))
            return resourceColor;

        return theme == AppTheme.Dark
            ? Color.FromArgb($"#{darkArgb:X6}")
            : Color.FromArgb($"#{lightArgb:X6}");
    }

    private static bool TryGetResourceColor(VisualElement? context, string key, out Color color)
    {
        for (var element = context; element is not null; element = element.Parent as VisualElement)
        {
            if (element.Resources.TryGetValue(key, out var value) && value is Color c)
            {
                color = c;
                return true;
            }
        }

        if (Application.Current?.Resources.TryGetValue(key, out var appValue) == true && appValue is Color appColor)
        {
            color = appColor;
            return true;
        }

        color = Colors.Transparent;
        return false;
    }
}

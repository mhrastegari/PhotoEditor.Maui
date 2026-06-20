namespace PhotoEditor.Maui;

/// <summary>Editor shell colors, metrics, and built-in toolbar theming; null values fall back to the app theme.</summary>
public sealed class PhotoEditorThemeOptions
{
    /// <summary>Primary accent for Done/Apply and active tool highlights; null uses app Primary.</summary>
    public Color? AccentColor { get; set; }

    /// <summary>Neutral surface for swatches and secondary panels; null uses app Secondary.</summary>
    public Color? SurfaceColor { get; set; }

    /// <summary>Primary label text color; null uses the app gray scale.</summary>
    public Color? TextPrimaryColor { get; set; }

    /// <summary>Secondary label and tool text color; null uses the app gray scale.</summary>
    public Color? TextSecondaryColor { get; set; }

    /// <summary>Text color on primary action buttons; null uses White or PrimaryDarkText.</summary>
    public Color? DoneButtonTextColor { get; set; }

    /// <summary>Editor shell background; null is transparent so the host page shows through.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>Top toolbar background; null uses app Secondary.</summary>
    public Color? ToolbarBackgroundColor { get; set; }

    /// <summary>Draw color and font-size palette background; null uses the app gray scale.</summary>
    public Color? PaletteBackgroundColor { get; set; }

    /// <summary>Outlined and secondary tool button fill; null uses app surface colors.</summary>
    public Color? SecondaryButtonBackgroundColor { get; set; }

    /// <summary>Button and shell border color; null uses the app gray scale.</summary>
    public Color? ButtonBorderColor { get; set; }

    /// <summary>Selected tool background fill; null derives a tint from <see cref="AccentColor"/>.</summary>
    public Color? ActiveToolBackgroundColor { get; set; }

    /// <summary>Selected tool border and text color; null uses the resolved accent.</summary>
    public Color? ActiveToolBorderColor { get; set; }

    /// <summary>Button corner radius in dip; null uses the default MAUI button radius (8).</summary>
    public double? ButtonCornerRadius { get; set; }

    /// <summary>Button and shell border width in dip; null uses 1.</summary>
    public double? ButtonBorderWidth { get; set; }

    /// <summary>Toolbar and palette shell corner radius in dip; null uses 8.</summary>
    public double? ToolbarCornerRadius { get; set; }

    /// <summary>Loading overlay scrim color; null uses a semi-transparent theme surface.</summary>
    public Color? LoadingOverlayColor { get; set; }

    /// <summary>Loading spinner color; null uses the resolved accent.</summary>
    public Color? LoadingIndicatorColor { get; set; }

    /// <summary>Canvas inset while a tool is active; null uses 32 dip horizontal margin.</summary>
    public Thickness? ToolModeCanvasMargin { get; set; }

    /// <summary>Canvas margin animation duration in milliseconds; null uses 220 ms.</summary>
    public uint? ToolModeMarginAnimationDurationMs { get; set; }

    /// <summary>Built-in toolbar button labels and optional icon font settings.</summary>
    public PhotoEditorToolbarOptions Toolbar { get; set; } = new();

    /// <summary>Creates a deep copy of these theme options.</summary>
    public PhotoEditorThemeOptions Clone() => new()
    {
        AccentColor = AccentColor,
        SurfaceColor = SurfaceColor,
        TextPrimaryColor = TextPrimaryColor,
        TextSecondaryColor = TextSecondaryColor,
        DoneButtonTextColor = DoneButtonTextColor,
        BackgroundColor = BackgroundColor,
        ToolbarBackgroundColor = ToolbarBackgroundColor,
        PaletteBackgroundColor = PaletteBackgroundColor,
        SecondaryButtonBackgroundColor = SecondaryButtonBackgroundColor,
        ButtonBorderColor = ButtonBorderColor,
        ActiveToolBackgroundColor = ActiveToolBackgroundColor,
        ActiveToolBorderColor = ActiveToolBorderColor,
        ButtonCornerRadius = ButtonCornerRadius,
        ButtonBorderWidth = ButtonBorderWidth,
        ToolbarCornerRadius = ToolbarCornerRadius,
        LoadingOverlayColor = LoadingOverlayColor,
        LoadingIndicatorColor = LoadingIndicatorColor,
        ToolModeCanvasMargin = ToolModeCanvasMargin,
        ToolModeMarginAnimationDurationMs = ToolModeMarginAnimationDurationMs,
        Toolbar = Toolbar.Clone(),
    };
}

namespace PhotoEditor.Maui;

/// <summary>
/// Cross-cutting configuration for <see cref="PhotoEditorView"/> and <see cref="SkiaPhotoEditorView"/>.
/// Set globally via <see cref="MauiAppBuilderExtensions.UsePhotoEditor"/> or per control in XAML/code.
/// </summary>
public sealed class PhotoEditorOptions
{
    public static PhotoEditorOptions Default { get; } = new();

    public PhotoEditorCanvasOptions Canvas { get; set; } = new();

    public PhotoEditorThemeOptions Theme { get; set; } = new();

    public PhotoEditorFeatureOptions Features { get; set; } = new();

    public PhotoEditorMessageOptions Messages { get; set; } = new();

    public static IReadOnlyList<Color> CreateDefaultDrawPalette() =>
    [
        Colors.Red,
        Colors.Goldenrod,
        Colors.DodgerBlue,
        Colors.Green,
        Colors.Purple,
        Colors.White,
        Colors.Black,
    ];

    public PhotoEditorOptions Clone() => new()
    {
        Canvas = Canvas.Clone(),
        Theme = Theme.Clone(),
        Features = Features.Clone(),
        Messages = Messages.Clone(),
    };
}

public sealed class PhotoEditorCanvasOptions
{
    public IList<Color> DrawColors { get; set; } = PhotoEditorOptions.CreateDefaultDrawPalette().ToList();

    public IList<float> TextFontSizes { get; set; } = [32f, 48f, 64f];

    public Color DefaultStrokeColor { get; set; } = Colors.Red;

    public float DefaultStrokeWidth { get; set; } = 6f;

    public float DrawStrokeWidthMin { get; set; } = 2f;

    public float DrawStrokeWidthMax { get; set; } = 24f;

    public float DefaultTextFontSize { get; set; } = 48f;

    public int MaxUndoHistoryDepth { get; set; } = 8;

    /// <summary>Maximum width of the working bitmap after load (pixels). Larger photos are downscaled.</summary>
    public double MaxEditWidth { get; set; } = 2048;

    /// <summary>Maximum height of the working bitmap after load (pixels). Larger photos are downscaled.</summary>
    public double MaxEditHeight { get; set; } = 2048;

    public PhotoEditorCropOptions Crop { get; set; } = new();

    public PhotoEditorArrowOptions Arrow { get; set; } = new();

    public PhotoEditorTextInputOptions TextInput { get; set; } = new();

    public PhotoEditorCanvasOptions Clone() => new()
    {
        DrawColors = DrawColors.ToList(),
        TextFontSizes = TextFontSizes.ToList(),
        DefaultStrokeColor = DefaultStrokeColor,
        DefaultStrokeWidth = DefaultStrokeWidth,
        DrawStrokeWidthMin = DrawStrokeWidthMin,
        DrawStrokeWidthMax = DrawStrokeWidthMax,
        DefaultTextFontSize = DefaultTextFontSize,
        MaxUndoHistoryDepth = MaxUndoHistoryDepth,
        MaxEditWidth = MaxEditWidth,
        MaxEditHeight = MaxEditHeight,
        Crop = Crop.Clone(),
        Arrow = Arrow.Clone(),
        TextInput = TextInput.Clone(),
    };
}

public sealed class PhotoEditorCropOptions
{
    public float MinCropSizeImagePx { get; set; } = 10f;

    public float HandleRadiusViewPx { get; set; } = 10f;

    public float HandleHitRadiusViewPx { get; set; } = 100f;

    public byte OverlayDimAlpha { get; set; } = 140;

    public Color OverlayBorderColor { get; set; } = Colors.White;

    public Color OverlayHandleColor { get; set; } = Colors.White;

    public float OverlayBorderWidth { get; set; } = 2f;

    public PhotoEditorCropOptions Clone() => new()
    {
        MinCropSizeImagePx = MinCropSizeImagePx,
        HandleRadiusViewPx = HandleRadiusViewPx,
        HandleHitRadiusViewPx = HandleHitRadiusViewPx,
        OverlayDimAlpha = OverlayDimAlpha,
        OverlayBorderColor = OverlayBorderColor,
        OverlayHandleColor = OverlayHandleColor,
        OverlayBorderWidth = OverlayBorderWidth,
    };
}

public sealed class PhotoEditorArrowOptions
{
    public float MinLengthViewPx { get; set; } = 12f;

    public float DefaultStrokeWidth { get; set; } = 6f;

    public float StrokeWidthMin { get; set; } = 2f;

    public float StrokeWidthMax { get; set; } = 32f;

    public uint HeadRevealDurationMs { get; set; } = 220;

    public PhotoEditorArrowOptions Clone() => new()
    {
        MinLengthViewPx = MinLengthViewPx,
        DefaultStrokeWidth = DefaultStrokeWidth,
        StrokeWidthMin = StrokeWidthMin,
        StrokeWidthMax = StrokeWidthMax,
        HeadRevealDurationMs = HeadRevealDurationMs,
    };
}

public sealed class PhotoEditorTextInputOptions
{
    public double EntryLayoutPaddingDip { get; set; } = 10;

    public double EntryMinWidthDip { get; set; } = 120;

    public Color BorderColor { get; set; } = Colors.White;

    public Color DragHandleColor { get; set; } = Colors.White;

    public PhotoEditorTextInputOptions Clone() => new()
    {
        EntryLayoutPaddingDip = EntryLayoutPaddingDip,
        EntryMinWidthDip = EntryMinWidthDip,
        BorderColor = BorderColor,
        DragHandleColor = DragHandleColor,
    };
}

public sealed class PhotoEditorThemeOptions
{
    /// <summary>Primary accent (Done/Apply, active tool border). Null uses app <c>Primary</c> / theme.</summary>
    public Color? AccentColor { get; set; }

    /// <summary>Neutral surface (e.g. font-size swatches). Null uses app <c>Secondary</c> / theme.</summary>
    public Color? SurfaceColor { get; set; }

    /// <summary>Primary label text. Null uses app gray scale / theme.</summary>
    public Color? TextPrimaryColor { get; set; }

    /// <summary>Secondary label and tool text. Null uses app gray scale / theme.</summary>
    public Color? TextSecondaryColor { get; set; }

    /// <summary>Text on primary buttons. Null uses <c>White</c> (light) or <c>PrimaryDarkText</c> (dark).</summary>
    public Color? DoneButtonTextColor { get; set; }

    /// <summary>Editor shell background. Null is transparent so the host page shows through.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>Top toolbar surface. Null uses app <c>Secondary</c> / theme.</summary>
    public Color? ToolbarBackgroundColor { get; set; }

    /// <summary>Color and font-size palette surface. Null uses app gray scale / theme.</summary>
    public Color? PaletteBackgroundColor { get; set; }

    /// <summary>Outlined / tool button fill. Null uses app surface colors / theme.</summary>
    public Color? SecondaryButtonBackgroundColor { get; set; }

    /// <summary>Button and shell border. Null uses app gray scale / theme.</summary>
    public Color? ButtonBorderColor { get; set; }

    /// <summary>Selected tool fill. Null derives a tint from <see cref="AccentColor"/>.</summary>
    public Color? ActiveToolBackgroundColor { get; set; }

    /// <summary>Selected tool border and text. Null uses resolved accent.</summary>
    public Color? ActiveToolBorderColor { get; set; }

    /// <summary>Button corner radius. Null uses the default MAUI button radius (8).</summary>
    public double? ButtonCornerRadius { get; set; }

    /// <summary>Button and shell border width. Null uses 1.</summary>
    public double? ButtonBorderWidth { get; set; }

    /// <summary>Toolbar and palette shell corner radius. Null uses 8.</summary>
    public double? ToolbarCornerRadius { get; set; }

    /// <summary>Loading overlay scrim. Null uses a semi-transparent theme surface.</summary>
    public Color? LoadingOverlayColor { get; set; }

    /// <summary>Loading spinner color. Null uses resolved accent.</summary>
    public Color? LoadingIndicatorColor { get; set; }

    /// <summary>Horizontal canvas inset while a tool is active. Null uses 32 dip horizontal.</summary>
    public Thickness? ToolModeCanvasMargin { get; set; }

    /// <summary>Canvas margin animation length. Null uses 220 ms.</summary>
    public uint? ToolModeMarginAnimationDurationMs { get; set; }

    /// <summary>Built-in toolbar labels and optional icon font (e.g. Material Icons).</summary>
    public PhotoEditorToolbarOptions Toolbar { get; set; } = new();

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

/// <summary>
/// Customizes built-in toolbar button content. When <see cref="IconFontFamily"/> is set, labels are
/// interpreted as icon font glyphs. Otherwise they are shown as button text.
/// </summary>
public sealed class PhotoEditorToolbarOptions
{
    /// <summary>Registered MAUI font family for icon glyphs (e.g. <c>MaterialIcons</c>). Null uses text labels.</summary>
    public string? IconFontFamily { get; set; }

    /// <summary>Icon glyph size in dip. Null uses 24.</summary>
    public double? IconSize { get; set; }

    public string Undo { get; set; } = "↶";
    public string Redo { get; set; } = "↷";
    public string Crop { get; set; } = "Crop";
    public string Draw { get; set; } = "Draw";
    public string Arrow { get; set; } = "Arrow";
    public string Text { get; set; } = "Text";
    public string RotateCrop { get; set; } = "↻";
    public string CancelTool { get; set; } = "Cancel";
    public string ApplyTool { get; set; } = "Apply";
    public string Cancel { get; set; } = "Cancel";
    public string Done { get; set; } = "Done";

    public PhotoEditorToolbarOptions Clone() => new()
    {
        IconFontFamily = IconFontFamily,
        IconSize = IconSize,
        Undo = Undo,
        Redo = Redo,
        Crop = Crop,
        Draw = Draw,
        Arrow = Arrow,
        Text = Text,
        RotateCrop = RotateCrop,
        CancelTool = CancelTool,
        ApplyTool = ApplyTool,
        Cancel = Cancel,
        Done = Done,
    };
}

public sealed class PhotoEditorFeatureOptions
{
    public bool Crop { get; set; } = true;

    public bool Draw { get; set; } = true;

    public bool Arrow { get; set; } = true;

    public bool Text { get; set; } = true;

    public bool UndoRedo { get; set; } = true;

    public PhotoEditorFeatureOptions Clone() => new()
    {
        Crop = Crop,
        Draw = Draw,
        Arrow = Arrow,
        Text = Text,
        UndoRedo = UndoRedo,
    };
}

public sealed class PhotoEditorMessageOptions
{
    public string DiscardTitle { get; set; } = "Discard changes?";

    public string DiscardMessage { get; set; } = "Your edits will be lost.";

    public string DiscardConfirm { get; set; } = "Discard";

    public string DiscardCancel { get; set; } = "Keep editing";

    public PhotoEditorMessageOptions Clone() => new()
    {
        DiscardTitle = DiscardTitle,
        DiscardMessage = DiscardMessage,
        DiscardConfirm = DiscardConfirm,
        DiscardCancel = DiscardCancel,
    };
}

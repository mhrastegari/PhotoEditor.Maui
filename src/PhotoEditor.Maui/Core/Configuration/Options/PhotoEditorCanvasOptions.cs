namespace PhotoEditor.Maui;

/// <summary>Canvas editing defaults, limits, and per-tool option groups.</summary>
public sealed class PhotoEditorCanvasOptions
{
    /// <summary>Colors shown in the built-in draw color palette.</summary>
    public IList<Color> DrawColors { get; set; } = PhotoEditorOptions.CreateDefaultDrawPalette().ToList();

    /// <summary>Font sizes offered in the built-in text size palette.</summary>
    public IList<float> TextFontSizes { get; set; } = [32f, 48f, 64f];

    /// <summary>Initial stroke color when draw or arrow mode starts.</summary>
    public Color DefaultStrokeColor { get; set; } = Colors.Red;

    /// <summary>Initial stroke width when draw mode starts.</summary>
    public float DefaultStrokeWidth { get; set; } = 6f;

    /// <summary>Minimum draw stroke width the user can select.</summary>
    public float DrawStrokeWidthMin { get; set; } = 2f;

    /// <summary>Maximum draw stroke width the user can select.</summary>
    public float DrawStrokeWidthMax { get; set; } = 24f;

    /// <summary>Initial font size when text mode starts.</summary>
    public float DefaultTextFontSize { get; set; } = 48f;

    /// <summary>Maximum number of edit snapshots kept on the undo stack.</summary>
    public int MaxUndoHistoryDepth { get; set; } = 8;

    /// <summary>Maximum working bitmap width in pixels; larger images are downscaled on load.</summary>
    public double MaxEditWidth { get; set; } = 2048;

    /// <summary>Maximum working bitmap height in pixels; larger images are downscaled on load.</summary>
    public double MaxEditHeight { get; set; } = 2048;

    /// <summary>Crop overlay appearance and interaction tuning.</summary>
    public PhotoEditorCropOptions Crop { get; set; } = new();

    /// <summary>Arrow annotation drawing and animation settings.</summary>
    public PhotoEditorArrowOptions Arrow { get; set; } = new();

    /// <summary>Inline text input overlay layout and colors.</summary>
    public PhotoEditorTextInputOptions TextInput { get; set; } = new();

    /// <summary>Creates a deep copy of this canvas options graph.</summary>
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

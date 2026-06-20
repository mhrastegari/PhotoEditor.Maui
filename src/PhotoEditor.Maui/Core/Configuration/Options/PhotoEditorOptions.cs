namespace PhotoEditor.Maui;

/// <summary>Root configuration for the photo editor; set globally or per control.</summary>
public sealed class PhotoEditorOptions
{
    /// <summary>Shared default instance used when no custom options are supplied.</summary>
    public static PhotoEditorOptions Default { get; } = new();

    /// <summary>Drawing, text, crop, arrow, and canvas behavior settings.</summary>
    public PhotoEditorCanvasOptions Canvas { get; set; } = new();

    /// <summary>Colors, layout metrics, and built-in toolbar appearance.</summary>
    public PhotoEditorThemeOptions Theme { get; set; } = new();

    /// <summary>Feature toggles for crop, draw, arrow, text, and undo/redo.</summary>
    public PhotoEditorFeatureOptions Features { get; set; } = new();

    /// <summary>User-facing dialog and confirmation strings.</summary>
    public PhotoEditorMessageOptions Messages { get; set; } = new();

    /// <summary>Returns the default draw color palette for new editor instances.</summary>
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

    /// <summary>Creates a deep copy of this options graph.</summary>
    public PhotoEditorOptions Clone() => new()
    {
        Canvas = Canvas.Clone(),
        Theme = Theme.Clone(),
        Features = Features.Clone(),
        Messages = Messages.Clone(),
    };
}

namespace PhotoEditor.Maui;

/// <summary>Built-in toolbar button labels; icon font glyphs are used when <see cref="IconFontFamily"/> is set.</summary>
public sealed class PhotoEditorToolbarOptions
{
    /// <summary>Registered MAUI font family for icon glyphs; null shows text labels instead.</summary>
    public string? IconFontFamily { get; set; }

    /// <summary>Icon glyph size in dip; null uses 24.</summary>
    public double? IconSize { get; set; }

    /// <summary>Undo toolbar button label or icon glyph.</summary>
    public string Undo { get; set; } = "↶";

    /// <summary>Redo toolbar button label or icon glyph.</summary>
    public string Redo { get; set; } = "↷";

    /// <summary>Crop tool button label or icon glyph.</summary>
    public string Crop { get; set; } = "Crop";

    /// <summary>Draw tool button label or icon glyph.</summary>
    public string Draw { get; set; } = "Draw";

    /// <summary>Arrow tool button label or icon glyph.</summary>
    public string Arrow { get; set; } = "Arrow";

    /// <summary>Text tool button label or icon glyph.</summary>
    public string Text { get; set; } = "Text";

    /// <summary>Rotate-crop button label or icon glyph, shown while crop mode is active.</summary>
    public string RotateCrop { get; set; } = "↻";

    /// <summary>Cancel button label in the per-tool action bar.</summary>
    public string CancelTool { get; set; } = "Cancel";

    /// <summary>Apply button label in the per-tool action bar.</summary>
    public string ApplyTool { get; set; } = "Apply";

    /// <summary>Cancel button label on the main editor action bar.</summary>
    public string Cancel { get; set; } = "Cancel";

    /// <summary>Done button label on the main editor action bar.</summary>
    public string Done { get; set; } = "Done";

    /// <summary>Creates a copy of these toolbar options.</summary>
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

namespace PhotoEditor.Maui;

/// <summary>Inline text entry overlay sizing and chrome colors.</summary>
public sealed class PhotoEditorTextInputOptions
{
    /// <summary>Padding around the text entry inside its host border, in dip.</summary>
    public double EntryLayoutPaddingDip { get; set; } = 10;

    /// <summary>Minimum width of the text entry host, in dip.</summary>
    public double EntryMinWidthDip { get; set; } = 120;

    /// <summary>Border color of the active text input host.</summary>
    public Color BorderColor { get; set; } = Colors.White;

    /// <summary>Color of the drag handle shown above the text entry.</summary>
    public Color DragHandleColor { get; set; } = Colors.White;

    /// <summary>Creates a copy of these text input options.</summary>
    public PhotoEditorTextInputOptions Clone() => new()
    {
        EntryLayoutPaddingDip = EntryLayoutPaddingDip,
        EntryMinWidthDip = EntryMinWidthDip,
        BorderColor = BorderColor,
        DragHandleColor = DragHandleColor,
    };
}

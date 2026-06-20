namespace PhotoEditor.Maui;

/// <summary>Enables or disables individual editor tools and undo/redo.</summary>
public sealed class PhotoEditorFeatureOptions
{
    /// <summary>Whether the crop tool is available.</summary>
    public bool Crop { get; set; } = true;

    /// <summary>Whether the freehand draw tool is available.</summary>
    public bool Draw { get; set; } = true;

    /// <summary>Whether the arrow annotation tool is available.</summary>
    public bool Arrow { get; set; } = true;

    /// <summary>Whether the text overlay tool is available.</summary>
    public bool Text { get; set; } = true;

    /// <summary>Whether undo and redo controls are available.</summary>
    public bool UndoRedo { get; set; } = true;

    /// <summary>Creates a copy of these feature flags.</summary>
    public PhotoEditorFeatureOptions Clone() => new()
    {
        Crop = Crop,
        Draw = Draw,
        Arrow = Arrow,
        Text = Text,
        UndoRedo = UndoRedo,
    };
}

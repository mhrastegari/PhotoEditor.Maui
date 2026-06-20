namespace PhotoEditor.Maui;

/// <summary>Strings shown in the discard-changes confirmation dialog.</summary>
public sealed class PhotoEditorMessageOptions
{
    /// <summary>Title of the discard-changes dialog.</summary>
    public string DiscardTitle { get; set; } = "Discard changes?";

    /// <summary>Body message explaining that edits will be lost.</summary>
    public string DiscardMessage { get; set; } = "Your edits will be lost.";

    /// <summary>Label for the button that confirms discarding edits.</summary>
    public string DiscardConfirm { get; set; } = "Discard";

    /// <summary>Label for the button that keeps the user in the editor.</summary>
    public string DiscardCancel { get; set; } = "Keep editing";

    /// <summary>Creates a copy of these message strings.</summary>
    public PhotoEditorMessageOptions Clone() => new()
    {
        DiscardTitle = DiscardTitle,
        DiscardMessage = DiscardMessage,
        DiscardConfirm = DiscardConfirm,
        DiscardCancel = DiscardCancel,
    };
}

namespace PhotoEditor.Maui.Sample.Themes;

internal static class MaterialIconsToolbar
{
    public static PhotoEditorToolbarOptions Create(string fontFamily, double iconSize = 24) => new()
    {
        IconFontFamily = fontFamily,
        IconSize = iconSize,
        Undo = "\ue166",
        Redo = "\ue15a",
        Crop = "\ue3be",
        Draw = "\ue3ae",
        Arrow = "\ue762",
        Text = "\ue264",
        RotateCrop = "\ue41a",
        CancelTool = "\ue5cd",
        ApplyTool = "\ue5ca",
        Cancel = "\ue5cd",
        Done = "\ue876",
    };
}

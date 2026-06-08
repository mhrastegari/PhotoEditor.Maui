namespace PhotoEditor.Maui;

public partial class PhotoEditorView
{
    public static readonly BindableProperty OutputFormatProperty = BindableProperty.Create(
        nameof(OutputFormat),
        typeof(PhotoEditorOutputFormat),
        typeof(PhotoEditorView),
        PhotoEditorOutputFormat.Png,
        propertyChanged: OnOutputSettingChanged);

    public static readonly BindableProperty OutputQualityProperty = BindableProperty.Create(
        nameof(OutputQuality),
        typeof(int),
        typeof(PhotoEditorView),
        SkiaPhotoEditorView.DefaultOutputQuality,
        validateValue: (_, value) => value is int quality && quality is >= 1 and <= 100,
        propertyChanged: OnOutputSettingChanged);

    public PhotoEditorOutputFormat OutputFormat
    {
        get => (PhotoEditorOutputFormat)GetValue(OutputFormatProperty);
        set => SetValue(OutputFormatProperty, value);
    }

    public int OutputQuality
    {
        get => (int)GetValue(OutputQualityProperty);
        set => SetValue(OutputQualityProperty, value);
    }

    public Task<MemoryStream?> GetEditedImageStreamAsync(CancellationToken cancellationToken = default) =>
        PhotoEditorControl.GetEditedImageStreamAsync(MaxOutputWidth, MaxOutputHeight, cancellationToken);

    private static void OnOutputSettingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhotoEditorView editor)
            return;

        editor.SyncOutputSettingsToPhotoEditor();
    }

    private void SyncOutputSettingsToPhotoEditor()
    {
        PhotoEditorControl.OutputFormat = OutputFormat;
        PhotoEditorControl.OutputQuality = OutputQuality;
    }
}

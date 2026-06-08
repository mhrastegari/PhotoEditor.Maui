using PhotoEditor.Maui.Sample.Services;

namespace PhotoEditor.Maui.Sample.Pages;

[QueryProperty(nameof(ImagePath), nameof(ImagePath))]
public partial class BuiltInEditorPage : ContentPage
{
    private string _imagePath = string.Empty;

    public string ImagePath
    {
        get => _imagePath;
        set
        {
            _imagePath = value;
            if (!string.IsNullOrWhiteSpace(_imagePath))
                Editor.ImageSourcePath = _imagePath;
        }
    }

    public BuiltInEditorPage()
    {
        InitializeComponent();
        Editor.SaveImageAsync = () => SampleSaveHelper.SaveWithPickerAsync(Editor);
        Editor.Completed += OnEditorCompleted;
    }

    private async void OnEditorCompleted(object? sender, string? savedPath)
    {
        if (string.IsNullOrWhiteSpace(savedPath))
        {
            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
            return;
        }

        await DisplayAlertAsync("Saved", savedPath, "OK").ConfigureAwait(true);
        Editor.ImageSourcePath = string.Empty;
        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }
}

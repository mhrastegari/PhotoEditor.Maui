namespace PhotoEditor.Maui.Sample.Pages;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnPickBuiltInClicked(object? sender, EventArgs e) =>
        await PickAndNavigateAsync(nameof(BuiltInEditorPage), nameof(BuiltInEditorPage.ImagePath)).ConfigureAwait(true);

    private async void OnPickThemedDefaultClicked(object? sender, EventArgs e) =>
        await PickAndNavigateAsync(nameof(ThemedDefaultEditorPage), nameof(ThemedDefaultEditorPage.ImagePath)).ConfigureAwait(true);

    private async void OnPickCustomClicked(object? sender, EventArgs e) =>
        await PickAndNavigateAsync(nameof(CustomEditorPage), nameof(CustomEditorPage.ImagePath)).ConfigureAwait(true);

    private static async Task PickAndNavigateAsync(string route, string pathParameter)
    {
        var path = await PickPhotoPathAsync().ConfigureAwait(true);
        if (path is null)
            return;

        await Shell.Current.GoToAsync($"{route}?{pathParameter}={Uri.EscapeDataString(path)}").ConfigureAwait(true);
    }

    private static async Task<string?> PickPhotoPathAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a photo",
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(true);

        return result?.FullPath;
    }
}

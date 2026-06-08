using Microsoft.Maui.Controls.Shapes;
using PhotoEditor.Maui.Sample.Services;
using PhotoEditor.Maui.Sample.Themes;

namespace PhotoEditor.Maui.Sample.Pages;

[QueryProperty(nameof(ImagePath), nameof(ImagePath))]
public partial class CustomEditorPage : ContentPage
{
    private string _imagePath = string.Empty;
    private bool _isSaving;

    public string ImagePath
    {
        get => _imagePath;
        set
        {
            _imagePath = value;
            if (!string.IsNullOrWhiteSpace(_imagePath))
                PhotoEditorControl.ImageSourcePath = _imagePath;
        }
    }

    public CustomEditorPage()
    {
        InitializeComponent();
        PhotoEditorControl.Options = CustomEditorTheme.CreateOptions();
        PhotoEditorControl.ShowBuiltInToolbar = false;
        PhotoEditorControl.Completed += OnEditorCompleted;
        PhotoEditorControl.InteractionModeChanged += (_, _) => RefreshChrome();
        PhotoEditorControl.ImageLoaded += (_, _) => RefreshChrome();
        PhotoEditorControl.EditingStateChanged += (_, _) => RefreshChrome();
        PhotoEditorControl.PhotoEditor.DrawingHistoryChanged += (_, _) => RefreshHistoryButtons();
        BuildColorPalette();
        BuildFontSizePalette();
        ConfigureStrokeSizeSlider();
    }

    private void ConfigureStrokeSizeSlider()
    {
        var canvas = PhotoEditorControl.Options.Canvas;
        PhotoEditorControl.PhotoEditor.DrawStrokeWidth = canvas.DefaultStrokeWidth;
        PhotoEditorControl.PhotoEditor.ArrowStrokeWidth = canvas.Arrow.DefaultStrokeWidth;
    }

    private void OnStrokeSizeSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)Math.Round(e.NewValue, 1);
        StrokeSizeLabel.Text = $"{value:0}";

        if (PhotoEditorControl.InteractionMode == SkiaPhotoEditorInteractionMode.Arrow)
            PhotoEditorControl.PhotoEditor.ArrowStrokeWidth = value;
        else
            PhotoEditorControl.PhotoEditor.DrawStrokeWidth = value;
    }

    private void SyncStrokeSizeSliderToMode()
    {
        var canvas = PhotoEditorControl.Options.Canvas;
        var isArrow = PhotoEditorControl.InteractionMode == SkiaPhotoEditorInteractionMode.Arrow;
        var arrow = canvas.Arrow;

        if (isArrow)
        {
            StrokeSizeSlider.Minimum = arrow.StrokeWidthMin;
            StrokeSizeSlider.Maximum = arrow.StrokeWidthMax;
            StrokeSizeSlider.Value = PhotoEditorControl.PhotoEditor.ArrowStrokeWidth;
        }
        else
        {
            StrokeSizeSlider.Minimum = canvas.DrawStrokeWidthMin;
            StrokeSizeSlider.Maximum = canvas.DrawStrokeWidthMax;
            StrokeSizeSlider.Value = PhotoEditorControl.PhotoEditor.DrawStrokeWidth;
        }

        StrokeSizeLabel.Text = $"{(float)Math.Round(StrokeSizeSlider.Value, 1):0}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshChrome();
    }

    private void BuildColorPalette()
    {
        ColorPalette.Children.Clear();
        foreach (var color in PhotoEditorControl.Options.Canvas.DrawColors)
        {
            var swatch = PhotoEditorColorSwatch.Create(color, size: 36, cornerRadius: 18);
            swatch.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    PhotoEditorControl.PhotoEditor.StrokeColor = color;
                    HighlightSwatch(ColorPalette, swatch, CustomEditorTheme.Accent);
                    PhotoEditorControl.PhotoEditor.RestoreTextInputFocus();
                })
            });
            ColorPalette.Children.Add(swatch);
        }

        if (ColorPalette.Children[0] is Border first)
            HighlightSwatch(ColorPalette, first, CustomEditorTheme.Accent);
    }

    private void BuildFontSizePalette()
    {
        FontSizePalette.Children.Clear();
        var fontSizes = PhotoEditorControl.Options.Canvas.TextFontSizes;

        for (var i = 0; i < fontSizes.Count; i++)
        {
            var size = fontSizes[i];
            var swatch = new Border
            {
                WidthRequest = 48,
                HeightRequest = 48,
                BackgroundColor = CustomEditorTheme.Surface,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Content = new Label
                {
                    Text = $"{size:0}",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = ComputeFontSizePreview(size, fontSizes),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = CustomEditorTheme.Text
                }
            };
            PhotoEditorColorSwatch.ApplyNeutralEdge(swatch);
            swatch.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    PhotoEditorControl.PhotoEditor.TextFontSize = size;
                    HighlightSwatch(FontSizePalette, swatch, CustomEditorTheme.Accent);
                    PhotoEditorControl.PhotoEditor.RestoreTextInputFocus();
                })
            });
            FontSizePalette.Children.Add(swatch);
        }

        var defaultIndex = 1;
        for (var i = 0; i < fontSizes.Count; i++)
        {
            if (Math.Abs(fontSizes[i] - PhotoEditorControl.Options.Canvas.DefaultTextFontSize) < 0.01f)
            {
                defaultIndex = i;
                break;
            }
        }

        PhotoEditorControl.PhotoEditor.TextFontSize = fontSizes[defaultIndex];
        if (FontSizePalette.Children[defaultIndex] is Border defaultSwatch)
            HighlightSwatch(FontSizePalette, defaultSwatch, CustomEditorTheme.Accent);
    }

    private static double ComputeFontSizePreview(float size, IList<float> all)
    {
        const double minPreview = 10;
        const double maxPreview = 20;
        var min = all.Min();
        var max = all.Max();
        if (Math.Abs(max - min) < 0.01f)
            return 14;

        return minPreview + (maxPreview - minPreview) * (size - min) / (max - min);
    }

    private static void HighlightSwatch(Layout layout, Border selected, Color accentColor)
    {
        foreach (var child in layout.Children)
        {
            if (child is Border border)
                PhotoEditorColorSwatch.SetSelected(border, ReferenceEquals(child, selected), accentColor);
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (await PhotoEditorControl.TryCancelWithConfirmationAsync().ConfigureAwait(true))
            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isSaving || !PhotoEditorControl.PhotoEditor.IsImageLoaded)
            return;

        _isSaving = true;
        SaveButton.IsEnabled = false;
        try
        {
            await PhotoEditorControl.CommitPendingEditsAsync().ConfigureAwait(true);

            var path = await SampleSaveHelper.SaveWithPickerAsync(PhotoEditorControl).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(path))
                return;

            await DisplayAlertAsync("Saved", path, "OK").ConfigureAwait(true);
            PhotoEditorControl.ImageSourcePath = string.Empty;
            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        finally
        {
            _isSaving = false;
            SaveButton.IsEnabled = true;
            RefreshChrome();
        }
    }

    private void OnCropToolClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.ToggleTool(SkiaPhotoEditorInteractionMode.Crop);

    private void OnDrawToolClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.ToggleTool(SkiaPhotoEditorInteractionMode.Draw);

    private void OnArrowToolClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.ToggleTool(SkiaPhotoEditorInteractionMode.Arrow);

    private void OnTextToolClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.ToggleTool(SkiaPhotoEditorInteractionMode.Text);

    private void OnUndoClicked(object? sender, EventArgs e) => PhotoEditorControl.UndoLastStroke();
    private void OnRedoClicked(object? sender, EventArgs e) => PhotoEditorControl.RedoLastStroke();

    private async void OnToolApplyClicked(object? sender, EventArgs e) =>
        await PhotoEditorControl.ApplyActiveToolAsync().ConfigureAwait(true);

    private async void OnRotateCropClicked(object? sender, EventArgs e)
    {
        if (PhotoEditorControl.InteractionMode == SkiaPhotoEditorInteractionMode.Crop)
            await PhotoEditorControl.RotateCropClockwiseAsync().ConfigureAwait(true);
    }

    private void OnToolCancelClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.CancelActiveTool();

    private async void OnEditorCompleted(object? sender, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            await DisplayAlertAsync("Saved", path, "OK").ConfigureAwait(true);

        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }

    private void RefreshChrome()
    {
        var mode = PhotoEditorControl.InteractionMode;
        var inTool = mode is SkiaPhotoEditorInteractionMode.Crop
            or SkiaPhotoEditorInteractionMode.Draw
            or SkiaPhotoEditorInteractionMode.Arrow
            or SkiaPhotoEditorInteractionMode.Text;

        var busy = _isSaving || PhotoEditorControl.PhotoEditor.IsLoading;
        var ready = !busy && PhotoEditorControl.PhotoEditor.IsImageLoaded;

        MainToolDock.IsVisible = !inTool;
        ToolModeBar.IsVisible = inTool;
        RotateCropButton.IsVisible = mode == SkiaPhotoEditorInteractionMode.Crop;

        var isDrawOrArrow = inTool && mode is SkiaPhotoEditorInteractionMode.Draw
            or SkiaPhotoEditorInteractionMode.Arrow;
        StrokeSizeHost.IsVisible = isDrawOrArrow;
        ColorPaletteHost.IsVisible = isDrawOrArrow;
        FontSizePaletteHost.IsVisible = inTool && mode == SkiaPhotoEditorInteractionMode.Text;

        if (isDrawOrArrow)
            SyncStrokeSizeSliderToMode();

        SaveButton.IsEnabled = ready;
        CloseButton.IsEnabled = !busy;

        SetToolButtonState(CropToolButton, mode == SkiaPhotoEditorInteractionMode.Crop, ready);
        SetToolButtonState(DrawToolButton, mode == SkiaPhotoEditorInteractionMode.Draw, ready);
        SetToolButtonState(ArrowToolButton, mode == SkiaPhotoEditorInteractionMode.Arrow, ready);
        SetToolButtonState(TextToolButton, mode == SkiaPhotoEditorInteractionMode.Text, ready);

        CropToolButton.IsEnabled = ready && !inTool;
        DrawToolButton.IsEnabled = ready && !inTool;
        ArrowToolButton.IsEnabled = ready && !inTool;
        TextToolButton.IsEnabled = ready && !inTool;

        ToolApplyButton.IsEnabled = ready;
        ToolCancelButton.IsEnabled = ready;
        RotateCropButton.IsEnabled = ready && mode == SkiaPhotoEditorInteractionMode.Crop;

        RefreshHistoryButtons();
    }

    private static void SetToolButtonState(Button button, bool active, bool enabled)
    {
        button.IsEnabled = enabled;
        button.BackgroundColor = active ? CustomEditorTheme.Accent : Color.FromArgb("#3D3D5C");
        button.TextColor = active ? Color.FromArgb("#1A1A2E") : CustomEditorTheme.Text;
    }

    private void RefreshHistoryButtons()
    {
        var ready = !_isSaving && PhotoEditorControl.PhotoEditor.IsImageLoaded && !PhotoEditorControl.PhotoEditor.IsLoading;
        UndoButton.IsEnabled = ready && PhotoEditorControl.PhotoEditor.CanUndo;
        RedoButton.IsEnabled = ready && PhotoEditorControl.PhotoEditor.CanRedo;
    }
}

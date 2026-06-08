using Microsoft.Maui.Controls.Shapes;

namespace PhotoEditor.Maui;

public partial class PhotoEditorView : ContentView
{
    public const double DefaultMaxOutputWidth = 1280.0;
    public const double DefaultMaxOutputHeight = 720.0;

    private const string EditorMarginAnimationKey = "EditorMargin";

    private static readonly Thickness DefaultEditorMargin = new(0);
    private static readonly TimeSpan InitializeDelay = TimeSpan.FromMilliseconds(50);

    private TaskCompletionSource<string?> _resultTcs = new();
    private string _sourcePath = string.Empty;
    private bool _closed;
    private bool _loaded;
    private bool _isSaving;
    private bool _isEditorInitializing = true;

    public PhotoEditorView()
    {
        InitializeComponent();
        WirePhotoEditorEvents();
        PhotoEditorControl.Options = Options;
        SyncOutputSettingsToPhotoEditor();
        ApplyThemeFromOptions();
        UpdateToolbarForInteractionMode();
        ApplyBuiltInToolbarVisibility();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SubscribeThemeChanges();
    }

    public PhotoEditorView(string imagePath) : this() => ImageSourcePath = imagePath;

    public Task<string?> ResultTask => _resultTcs.Task;

    public static readonly BindableProperty ImageSourcePathProperty = BindableProperty.Create(
        nameof(ImageSourcePath), typeof(string), typeof(PhotoEditorView), string.Empty, propertyChanged: OnImageSourcePathChanged);

    public static readonly BindableProperty MaxOutputWidthProperty = BindableProperty.Create(
        nameof(MaxOutputWidth), typeof(double), typeof(PhotoEditorView), DefaultMaxOutputWidth, validateValue: (_, value) => value is double d && d > 0);

    public static readonly BindableProperty MaxOutputHeightProperty = BindableProperty.Create(
        nameof(MaxOutputHeight), typeof(double), typeof(PhotoEditorView), DefaultMaxOutputHeight, validateValue: (_, value) => value is double d && d > 0);

    public static readonly BindableProperty ShowBuiltInToolbarProperty = BindableProperty.Create(
        nameof(ShowBuiltInToolbar), typeof(bool), typeof(PhotoEditorView), true,
        propertyChanged: (b, _, _) => { if (b is PhotoEditorView editor) editor.ApplyBuiltInToolbarVisibility(); });

    public static readonly BindableProperty ConfirmDiscardOnCancelProperty = BindableProperty.Create(
        nameof(ConfirmDiscardOnCancel), typeof(bool), typeof(PhotoEditorView), true);

    public string ImageSourcePath { get => (string)GetValue(ImageSourcePathProperty); set => SetValue(ImageSourcePathProperty, value); }
    public double MaxOutputWidth { get => (double)GetValue(MaxOutputWidthProperty); set => SetValue(MaxOutputWidthProperty, value); }
    public double MaxOutputHeight { get => (double)GetValue(MaxOutputHeightProperty); set => SetValue(MaxOutputHeightProperty, value); }
    public bool ShowBuiltInToolbar { get => (bool)GetValue(ShowBuiltInToolbarProperty); set => SetValue(ShowBuiltInToolbarProperty, value); }
    public bool ConfirmDiscardOnCancel { get => (bool)GetValue(ConfirmDiscardOnCancelProperty); set => SetValue(ConfirmDiscardOnCancelProperty, value); }

    public Func<Task<bool>>? ConfirmDiscardAsync { get; set; }

    /// <summary>When set, invoked on Done after pending edits are committed. Return null to cancel saving.</summary>
    public Func<Task<string?>>? SaveImageAsync { get; set; }

    public SkiaPhotoEditorView PhotoEditor => PhotoEditorControl;
    public SkiaPhotoEditorInteractionMode InteractionMode => PhotoEditorControl.InteractionMode;

    public event EventHandler? InteractionModeChanged;
    public event EventHandler? ImageLoaded;
    public event EventHandler? EditingStateChanged;
    public event EventHandler<string?>? Completed;

    public void StartCropMode() => PhotoEditorControl.StartCropMode();
    public void CancelCropMode() => PhotoEditorControl.CancelCropMode();
    public Task ApplyCropAsync() => PhotoEditorControl.ApplyCropAsync();
    public Task RotateCropClockwiseAsync() => PhotoEditorControl.RotateCropClockwiseAsync();
    public void StartDrawMode() => PhotoEditorControl.StartDrawMode();
    public void StopDrawMode() => PhotoEditorControl.StopDrawMode();
    public void StartArrowMode() => PhotoEditorControl.StartArrowMode();
    public void StopArrowMode() => PhotoEditorControl.StopArrowMode();
    public void ApplyDrawMode() => PhotoEditorControl.ApplyDrawMode();
    public void CancelDrawMode() => PhotoEditorControl.CancelDrawMode();
    public void ApplyArrowMode() => PhotoEditorControl.ApplyArrowMode();
    public void CancelArrowMode() => PhotoEditorControl.CancelArrowMode();
    public void StartTextMode() => PhotoEditorControl.StartTextMode();
    public void StopTextMode() => PhotoEditorControl.StopTextMode();
    public void ApplyTextMode() => PhotoEditorControl.ApplyTextMode();
    public void CancelTextMode() => PhotoEditorControl.CancelTextMode();
    public void UndoLastStroke() => PhotoEditorControl.UndoLastStroke();
    public void RedoLastStroke() => PhotoEditorControl.RedoLastStroke();
    public void ToggleTool(SkiaPhotoEditorInteractionMode mode) => PhotoEditorControl.ToggleTool(mode);
    public void ActivateTool(SkiaPhotoEditorInteractionMode mode) => PhotoEditorControl.ActivateTool(mode);
    public void CancelActiveTool() => PhotoEditorControl.CancelActiveTool();
    public Task ApplyActiveToolAsync() => PhotoEditorControl.ApplyActiveToolAsync();

    public Task<MemoryStream?> GetEditedPngStreamAsync(CancellationToken cancellationToken = default) =>
        PhotoEditorControl.GetEditedPngStreamAsync(MaxOutputWidth, MaxOutputHeight, cancellationToken);

    public async Task<bool> DisplayDiscardAlertAsync(Page? page)
    {
        if (page is null)
            return true;

        var messages = Options.Messages;
        return await page.DisplayAlertAsync(
            messages.DiscardTitle,
            messages.DiscardMessage,
            messages.DiscardConfirm,
            messages.DiscardCancel).ConfigureAwait(true);
    }

    public async Task<bool> TryCancelWithConfirmationAsync()
    {
        if (_closed) return true;
        if (ConfirmDiscardOnCancel)
        {
            var confirm = ConfirmDiscardAsync ?? (() => DisplayDiscardAlertAsync(GetHostPage()));
            if (!await confirm().ConfigureAwait(true)) return false;
        }

        await CompleteAsync(null).ConfigureAwait(true);
        return true;
    }

    private static void OnImageSourcePathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhotoEditorView editor) return;

        var newPath = newValue as string ?? string.Empty;
        var oldPath = oldValue as string ?? string.Empty;
        if (string.Equals(oldPath, newPath, StringComparison.Ordinal))
            return;

        editor._sourcePath = newPath;
        if (string.IsNullOrWhiteSpace(newPath))
            return;

        editor.PrepareForNewImage();
        if (editor._loaded)
            _ = editor.InitializeEditorDelayedAsync();
    }

    private void PrepareForNewImage()
    {
        _closed = false;
        _isSaving = false;
        _isEditorInitializing = false;

        if (_resultTcs.Task.IsCompleted)
            _resultTcs = new TaskCompletionSource<string?>();

        PhotoEditorControl.CancelCropMode();
        PhotoEditorControl.StopDrawMode();
        PhotoEditorControl.StopArrowMode();
        PhotoEditorControl.CancelTextMode();

        UpdateLoadingVisualState();
        UpdateToolbarForInteractionMode();
    }

    internal void ApplyToolbarLabels()
    {
        var toolbar = Theme.Toolbar;
        var useIcons = !string.IsNullOrWhiteSpace(toolbar.IconFontFamily);

        ApplyToolbarContent(UndoToolButton, toolbar.Undo, symbol: true, useIcon: useIcons);
        ApplyToolbarContent(RedoToolButton, toolbar.Redo, symbol: true, useIcon: useIcons);
        ApplyToolbarContent(CropToolButton, toolbar.Crop, useIcon: useIcons);
        ApplyToolbarContent(DrawToolButton, toolbar.Draw, useIcon: useIcons);
        ApplyToolbarContent(ArrowToolButton, toolbar.Arrow, useIcon: useIcons);
        ApplyToolbarContent(TextToolButton, toolbar.Text, useIcon: useIcons);
        ApplyToolbarContent(RotateCropToolButton, toolbar.RotateCrop, symbol: true, useIcon: useIcons);
        ApplyToolbarContent(CancelCropToolButton, toolbar.CancelTool, action: true, useIcon: useIcons);
        ApplyToolbarContent(ApplyCropToolButton, toolbar.ApplyTool, action: true, useIcon: useIcons);
        ApplyToolbarContent(CancelButton, toolbar.Cancel, action: true, useIcon: useIcons);
        ApplyToolbarContent(DoneButton, toolbar.Done, action: true, useIcon: useIcons);
        RefreshToolbarButtons();
    }

    private void ApplyToolbarContent(
        Button button,
        string label,
        bool symbol = false,
        bool action = false,
        bool useIcon = false)
    {
        if (useIcon)
            ConfigureIconButton(button, label);
        else
            ConfigureToolbarButton(button, label, symbol, action);
    }

    private void ConfigureIconButton(Button button, string glyph)
    {
        var toolbar = Theme.Toolbar;
        button.Text = string.Empty;
        button.ImageSource = new FontImageSource
        {
            FontFamily = toolbar.IconFontFamily,
            Glyph = glyph,
            Color = _resolvedTheme.TextSecondaryColor,
            Size = toolbar.IconSize ?? 24,
        };
        button.Padding = new Thickness(0);
    }

    private static void ConfigureToolbarButton(Button button, string label, bool symbol = false, bool action = false)
    {
        button.ImageSource = null;
        button.Text = label ?? string.Empty;
        button.FontAttributes = FontAttributes.Bold;
        button.FontSize = symbol ? 20 : action ? 15 : 13;
        button.Padding = action ? new Thickness(12, 0) : symbol ? new Thickness(0) : new Thickness(10, 0);
    }

    private void WirePhotoEditorEvents()
    {
        PhotoEditorControl.InteractionModeChanged += (_, _) => { UpdateToolbarForInteractionMode(); InteractionModeChanged?.Invoke(this, EventArgs.Empty); };
        PhotoEditorControl.ImageLoaded += (_, _) => ImageLoaded?.Invoke(this, EventArgs.Empty);
        PhotoEditorControl.EditingStateChanged += (_, _) => { UpdateLoadingVisualState(); EditingStateChanged?.Invoke(this, EventArgs.Empty); };
        PhotoEditorControl.DrawingHistoryChanged += (_, _) => UpdateToolbarHistoryState();
    }

    private void ApplyBuiltInToolbarVisibility()
    {
        BuiltInToolbar.IsVisible = ShowBuiltInToolbar;
        if (!ShowBuiltInToolbar)
        {
            DrawColorPaletteShell.IsVisible = false;
            TextFontSizePaletteShell.IsVisible = false;
        }
        else
            UpdateToolbarForInteractionMode();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        _sourcePath = ImageSourcePath;
        if (string.IsNullOrWhiteSpace(_sourcePath)) return;
        _ = InitializeEditorDelayedAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        UnsubscribeThemeChanges();
        PhotoEditorControl.AbortAnimation(EditorMarginAnimationKey);
        if (!_closed) CompleteWithoutClosing(null);
    }

    private async Task InitializeEditorDelayedAsync()
    {
        await Task.Delay(InitializeDelay).ConfigureAwait(true);
        if (_closed || Handler is null) return;

        _isEditorInitializing = true;
        UpdateLoadingVisualState();
        try
        {
            await PhotoEditorControl.LoadImageAsync(_sourcePath).ConfigureAwait(true);
            EnsureDrawColorPalette();
            EnsureTextFontSizePalette();
            UpdateToolbarForInteractionMode();
            UpdateToolbarHistoryState();
        }
        finally
        {
            _isEditorInitializing = false;
            UpdateLoadingVisualState();
        }
    }

    private void EnsureDrawColorPalette()
    {
        if (DrawColorPalette.Children.Count > 0) return;

        foreach (var color in Options.Canvas.DrawColors)
        {
            var swatch = PhotoEditorColorSwatch.Create(color);
            swatch.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    PhotoEditorControl.StrokeColor = color;
                    HighlightSelectedColorSwatch(swatch);
                    PhotoEditorControl.RestoreTextInputFocus();
                })
            });
            DrawColorPalette.Children.Add(swatch);
        }

        if (DrawColorPalette.Children[0] is Border first) HighlightSelectedColorSwatch(first);
    }

    private void EnsureTextFontSizePalette()
    {
        if (TextFontSizePalette.Children.Count > 0) return;

        var previewSizes = new[] { 14.0, 18.0, 22.0 };
        var fontSizes = Options.Canvas.TextFontSizes;

        for (var i = 0; i < fontSizes.Count; i++)
        {
            var selectedFontSize = fontSizes[i];
            var selectedSwatchIndex = i;
            var previewSize = i < previewSizes.Length ? previewSizes[i] : 18.0;

            var swatch = new Border
            {
                WidthRequest = 40,
                HeightRequest = 40,
                BackgroundColor = _resolvedTheme.SurfaceColor,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = new Label
                {
                    Text = "A",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = previewSize,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = _resolvedTheme.TextPrimaryColor
                }
            };

            PhotoEditorColorSwatch.ApplyNeutralEdge(swatch);

            swatch.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    PhotoEditorControl.TextFontSize = selectedFontSize;
                    if (TextFontSizePalette.Children[selectedSwatchIndex] is Border paletteSwatch)
                        HighlightSelectedFontSizeSwatch(paletteSwatch);
                    PhotoEditorControl.RestoreTextInputFocus();
                })
            });

            TextFontSizePalette.Children.Add(swatch);
        }

        var defaultIndex = 1;
        for (var i = 0; i < fontSizes.Count; i++)
        {
            if (Math.Abs(fontSizes[i] - Options.Canvas.DefaultTextFontSize) < 0.01f)
            {
                defaultIndex = i;
                break;
            }
        }
        PhotoEditorControl.TextFontSize = fontSizes[defaultIndex];
        if (TextFontSizePalette.Children[defaultIndex] is Border defaultSwatch) HighlightSelectedFontSizeSwatch(defaultSwatch);
    }

    private void HighlightBorder(Border border, bool selected) =>
        PhotoEditorColorSwatch.SetSelected(border, selected, _resolvedTheme.AccentColor);

    private void HighlightSelectedColorSwatch(Border selected)
    {
        foreach (var child in DrawColorPalette.Children)
            if (child is Border border)
                HighlightBorder(border, ReferenceEquals(child, selected));
    }

    private void HighlightSelectedFontSizeSwatch(Border selected)
    {
        foreach (var child in TextFontSizePalette.Children)
            if (child is Border border)
                HighlightBorder(border, ReferenceEquals(child, selected));
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await TryCancelWithConfirmationAsync().ConfigureAwait(true);

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        if (_isEditorInitializing) return;

        _isSaving = true;
        UpdateLoadingVisualState();
        try
        {
            var path = await SaveEditedImageAsync().ConfigureAwait(true);
            if (path is null)
                return;

            await CompleteAsync(path).ConfigureAwait(true);
        }
        catch
        {
            await CompleteAsync(null).ConfigureAwait(true);
        }
        finally
        {
            _isSaving = false;
            UpdateLoadingVisualState();
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

    private void OnUndoToolClicked(object? sender, EventArgs e) => PhotoEditorControl.UndoLastStroke();
    private void OnRedoToolClicked(object? sender, EventArgs e) => PhotoEditorControl.RedoLastStroke();

    private async void OnApplyCropToolClicked(object? sender, EventArgs e) =>
        await PhotoEditorControl.ApplyActiveToolAsync().ConfigureAwait(true);

    private async void OnRotateCropToolClicked(object? sender, EventArgs e)
    {
        if (PhotoEditorControl.InteractionMode == SkiaPhotoEditorInteractionMode.Crop)
            await PhotoEditorControl.RotateCropClockwiseAsync().ConfigureAwait(true);
    }

    private void OnCancelCropToolClicked(object? sender, EventArgs e) =>
        PhotoEditorControl.CancelActiveTool();

    public async Task CommitPendingEditsAsync()
    {
        if (!PhotoEditorControl.IsImageLoaded)
            return;

        await PhotoEditorControl.ApplyActiveToolAsync().ConfigureAwait(true);
    }

    private async Task<string?> SaveEditedImageAsync()
    {
        if (!PhotoEditorControl.IsImageLoaded)
            return null;

        await CommitPendingEditsAsync().ConfigureAwait(true);

        if (SaveImageAsync is not null)
            return await SaveImageAsync().ConfigureAwait(true);

        return await PhotoEditorControl.SaveEditedImageAsync(
            FileSystem.CacheDirectory,
            CreateSaveFileNameWithoutExtension(),
            MaxOutputWidth,
            MaxOutputHeight).ConfigureAwait(true);
    }

    private string CreateSaveFileNameWithoutExtension()
    {
        if (!string.IsNullOrWhiteSpace(ImageSourcePath))
            return $"{System.IO.Path.GetFileNameWithoutExtension(ImageSourcePath)}_edited";

        return $"edited_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private Task CompleteAsync(string? path)
    {
        if (_closed) return Task.CompletedTask;
        _closed = true;
        _resultTcs.TrySetResult(path);
        Completed?.Invoke(this, path);
        return Task.CompletedTask;
    }

    private void CompleteWithoutClosing(string? path)
    {
        if (_closed) return;
        _closed = true;
        _resultTcs.TrySetResult(path);
    }

    private Page? GetHostPage()
    {
        var element = (Element)this;
        while (element is not null)
        {
            if (element is Page page) return page;
            element = element.Parent;
        }

        return Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    private void UpdateLoadingVisualState()
    {
        if (Handler is null) return;

        var isBusy = _isEditorInitializing || _isSaving || PhotoEditorControl.IsLoading;
        DoneButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        DoneButton.Opacity = _isSaving ? 0.65 : 1;
        UpdateToolbarEnabledState(!isBusy && PhotoEditorControl.IsImageLoaded);
    }

    private void UpdateToolbarForInteractionMode()
    {
        var mode = PhotoEditorControl.InteractionMode;
        var isCrop = mode == SkiaPhotoEditorInteractionMode.Crop;
        var isDraw = mode == SkiaPhotoEditorInteractionMode.Draw;
        var isArrow = mode == SkiaPhotoEditorInteractionMode.Arrow;
        var isText = mode == SkiaPhotoEditorInteractionMode.Text;
        var inToolMode = isCrop || isDraw || isArrow || isText;
        var showToolbar = ShowBuiltInToolbar;
        var features = Options.Features;

        AnimatePhotoEditorMargin(inToolMode);
        TopToolbarShell.IsVisible = showToolbar;
        HistoryToolbar.IsVisible = showToolbar && features.UndoRedo;
        UndoToolButton.IsVisible = showToolbar && features.UndoRedo;
        RedoToolButton.IsVisible = showToolbar && features.UndoRedo;
        CropToolButton.IsVisible = showToolbar && !inToolMode && features.Crop;
        DrawToolButton.IsVisible = showToolbar && !inToolMode && features.Draw;
        ArrowToolButton.IsVisible = showToolbar && !inToolMode && features.Arrow;
        TextToolButton.IsVisible = showToolbar && !inToolMode && features.Text;
        ToolActionBar.IsVisible = showToolbar && inToolMode;
        EditorActionBar.IsVisible = showToolbar && !inToolMode;
        RotateCropToolButton.IsVisible = isCrop;
        DrawColorPaletteShell.IsVisible = showToolbar && (isDraw || isArrow || isText);
        TextFontSizePaletteShell.IsVisible = showToolbar && isText;

        RefreshToolbarButtons();

        var canUseTools = !_isEditorInitializing && !_isSaving && !PhotoEditorControl.IsLoading && PhotoEditorControl.IsImageLoaded;
        RotateCropToolButton.IsEnabled = canUseTools && isCrop;

        UpdateToolbarHistoryState();
    }

    private void UpdateToolbarEnabledState(bool enabled)
    {
        CropToolButton.IsEnabled = enabled;
        DrawToolButton.IsEnabled = enabled;
        ArrowToolButton.IsEnabled = enabled;
        TextToolButton.IsEnabled = enabled;
        ApplyCropToolButton.IsEnabled = enabled;
        CancelCropToolButton.IsEnabled = enabled;
        RotateCropToolButton.IsEnabled = enabled && PhotoEditorControl.InteractionMode == SkiaPhotoEditorInteractionMode.Crop;
        UpdateToolbarHistoryState();
    }

    private void UpdateToolbarHistoryState()
    {
        if (Handler is null) return;

        var enabled = !_isEditorInitializing && !_isSaving && !PhotoEditorControl.IsLoading && PhotoEditorControl.IsImageLoaded;
        UndoToolButton.IsEnabled = enabled && PhotoEditorControl.CanUndo;
        RedoToolButton.IsEnabled = enabled && PhotoEditorControl.CanRedo;
        ApplyDisabledButtonStyle(UndoToolButton);
        ApplyDisabledButtonStyle(RedoToolButton);
    }

    private void AnimatePhotoEditorMargin(bool inToolMode)
    {
        var target = inToolMode ? _resolvedTheme.ToolModeCanvasMargin : DefaultEditorMargin;

        if (Handler is null)
        {
            PhotoEditorControl.Margin = target;
            return;
        }

        var start = PhotoEditorControl.Margin;
        if (Math.Abs(start.Left - target.Left) < 0.5 && Math.Abs(start.Right - target.Right) < 0.5 && Math.Abs(start.Top - target.Top) < 0.5 && Math.Abs(start.Bottom - target.Bottom) < 0.5)
            return;

        PhotoEditorControl.AbortAnimation(EditorMarginAnimationKey);

        var startLeft = start.Left;
        var startTop = start.Top;
        var startRight = start.Right;
        var startBottom = start.Bottom;

        new Animation(v =>
            {
                PhotoEditorControl.Margin = new Thickness(
                    startLeft + (target.Left - startLeft) * v,
                    startTop + (target.Top - startTop) * v,
                    startRight + (target.Right - startRight) * v,
                    startBottom + (target.Bottom - startBottom) * v);
            }, 0, 1, Easing.CubicInOut)
            .Commit(PhotoEditorControl, EditorMarginAnimationKey, 16, _resolvedTheme.ToolModeMarginAnimationDurationMs);
    }
}
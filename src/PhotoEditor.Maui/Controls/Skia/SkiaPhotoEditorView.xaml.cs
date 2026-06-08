using SkiaSharp;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView : ContentView
{
    private readonly List<StrokePath> _strokes = [];
    private readonly List<StrokePath> _redoStrokes = [];
    private readonly List<PhotoEditorArrowAnnotation> _arrows = [];
    private readonly List<PhotoEditorArrowAnnotation> _redoArrows = [];
    private readonly List<PhotoEditorTextOverlay> _textOverlays = [];
    private readonly List<PhotoEditorTextOverlay> _textRedoOverlays = [];
    private readonly List<EditSnapshot> _editUndoStack = [];
    private readonly List<EditSnapshot> _editRedoStack = [];
    private readonly object _bitmapLock = new();

    private SKBitmap? _bitmap;
    private StrokePath? _activeStroke;
    private CroppingRectangle? _croppingRect;
    private CropDragHandle _activeHandle = CropDragHandle.None;
    private SKRect _imageDestView;
    private SKSize _cropDragSize;
    private SKPoint _cropTouchOffset;
    private SKPoint _pendingTextBitmapCenter;
    private SKMatrix _bitmapMatrix = SKMatrix.Identity;
    private SKMatrix _inverseBitmapMatrix = SKMatrix.Identity;
    private Point _textInputPanLayoutOrigin;
    private PhotoEditorArrowAnnotation? _activeArrow;
    private PhotoEditorArrowAnnotation? _arrowHeadRevealTarget;
    private CancellationTokenSource? _arrowHeadRevealCts;
    private int _surfaceWidth;
    private int _surfaceHeight;
    private int _drawSessionStrokeCount;
    private int _arrowSessionArrowCount;
    private int _textSessionOverlayCount;
    private int _overlaySessionHistoryIndex = -1;
    private bool _isCommittingTextInput;
    private bool _isTextInputDragging;
    private bool _suppressTextInputUnfocusedCommit;
    private bool _croppingRectNeedsSync = true;
    private bool _isMutatingBitmap;
    private bool _isLoading;
    private float _lastBitmapDisplayScale = -1f;
    private int _loadGeneration;

    public SkiaPhotoEditorView()
    {
        InitializeComponent();
        TextInputEntry.TextChanged += OnTextInputEntryTextChanged;
        TextInputHost.SizeChanged += OnTextInputHostSizeChanged;
        var textInputPan = new PanGestureRecognizer();
        textInputPan.PanUpdated += OnTextInputPanUpdated;
        TextInputHost.GestureRecognizers.Add(textInputPan);
        var dragHandlePan = new PanGestureRecognizer();
        dragHandlePan.PanUpdated += OnTextInputPanUpdated;
        TextInputDragHandle.GestureRecognizers.Add(dragHandlePan);
        var entryPan = new PanGestureRecognizer();
        entryPan.PanUpdated += OnTextInputPanUpdated;
        TextInputEntry.GestureRecognizers.Add(entryPan);
        CanvasView.SizeChanged += OnCanvasViewSizeChanged;
        Unloaded += OnUnloaded;
        SubscribeThemeChanges();
        ApplyOptionsFromConfiguration();
    }

    private void OnCanvasViewSizeChanged(object? sender, EventArgs e) =>
        CanvasView.InvalidateSurface();

    public event EventHandler? InteractionModeChanged;
    public event EventHandler? ImageLoaded;
    public event EventHandler? EditingStateChanged;
    public event EventHandler? DrawingHistoryChanged;

    public bool CanUndo =>
        (InteractionMode == SkiaPhotoEditorInteractionMode.Draw && _strokes.Count > 0)
        || (InteractionMode == SkiaPhotoEditorInteractionMode.Arrow && _arrows.Count > _arrowSessionArrowCount)
        || (InteractionMode == SkiaPhotoEditorInteractionMode.Text && _textOverlays.Count > _textSessionOverlayCount)
        || _editUndoStack.Count > 0;

    public bool CanRedo =>
        (InteractionMode == SkiaPhotoEditorInteractionMode.Draw && _redoStrokes.Count > 0)
        || (InteractionMode == SkiaPhotoEditorInteractionMode.Arrow && _redoArrows.Count > 0)
        || (InteractionMode == SkiaPhotoEditorInteractionMode.Text && _textRedoOverlays.Count > 0)
        || _editRedoStack.Count > 0;

    public bool IsImageLoaded
    {
        get
        {
            lock (_bitmapLock)
                return _bitmap is { IsNull: false, Width: > 0, Height: > 0 };
        }
    }

    public SkiaPhotoEditorInteractionMode InteractionMode { get; private set; }

    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor),
        typeof(Color),
        typeof(SkiaPhotoEditorView),
        PhotoEditorOptions.Default.Canvas.DefaultStrokeColor,
        propertyChanged: OnDrawStrokeAppearancePropertyChanged);

    public static readonly BindableProperty DrawStrokeWidthProperty = BindableProperty.Create(
        nameof(DrawStrokeWidth),
        typeof(float),
        typeof(SkiaPhotoEditorView),
        PhotoEditorOptions.Default.Canvas.DefaultStrokeWidth,
        propertyChanged: OnDrawStrokeAppearancePropertyChanged);

    public static readonly BindableProperty ArrowStrokeWidthProperty = BindableProperty.Create(
        nameof(ArrowStrokeWidth),
        typeof(float),
        typeof(SkiaPhotoEditorView),
        PhotoEditorOptions.Default.Canvas.Arrow.DefaultStrokeWidth,
        propertyChanged: OnArrowStrokeWidthPropertyChanged);

    public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
        nameof(TextFontSize),
        typeof(float),
        typeof(SkiaPhotoEditorView),
        PhotoEditorOptions.Default.Canvas.DefaultTextFontSize,
        propertyChanged: OnTextFontSizePropertyChanged);

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public float DrawStrokeWidth
    {
        get => (float)GetValue(DrawStrokeWidthProperty);
        set => SetValue(DrawStrokeWidthProperty, value);
    }

    public float ArrowStrokeWidth
    {
        get => (float)GetValue(ArrowStrokeWidthProperty);
        set => SetValue(ArrowStrokeWidthProperty, value);
    }

    /// <summary>Alias for <see cref="DrawStrokeWidth"/>.</summary>
    public float StrokeWidth
    {
        get => DrawStrokeWidth;
        set => DrawStrokeWidth = value;
    }

    public float TextFontSize
    {
        get => (float)GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;

            _isLoading = value;
            LoadingOverlay.IsVisible = value;
            CanvasView.IsEnabled = !value && IsImageLoaded;
            EditingStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task LoadImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        return RunOnUiAsync(async () =>
        {
            var loadGeneration = Interlocked.Increment(ref _loadGeneration);
            IsLoading = true;
            InteractionMode = SkiaPhotoEditorInteractionMode.None;
            _strokes.Clear();
            _redoStrokes.Clear();
            _arrows.Clear();
            _redoArrows.Clear();
            _textOverlays.Clear();
            _textRedoOverlays.Clear();
            HideTextInputHost();
            ClearEditHistoryStacks();
            _activeStroke = null;
            _activeHandle = CropDragHandle.None;
            _activeArrow = null;
            RaiseInteractionModeChanged();
            RaiseDrawingHistoryChanged();

            try
            {
                var canvasOptions = EditorOptions.Canvas;
                var decoded = await Task.Run(
                        () => SkiaPhotoEditorBitmapHelper.DecodeFromFile(
                            imagePath,
                            canvasOptions.MaxEditWidth,
                            canvasOptions.MaxEditHeight),
                        cancellationToken)
                    .ConfigureAwait(true);

                if (loadGeneration != _loadGeneration || cancellationToken.IsCancellationRequested)
                {
                    decoded?.Dispose();
                    return;
                }

                if (decoded is null || decoded.IsNull)
                    return;

                lock (_bitmapLock)
                {
                    if (loadGeneration != _loadGeneration)
                    {
                        decoded.Dispose();
                        return;
                    }

                    _bitmap?.Dispose();
                    _bitmap = decoded;
                }

                _croppingRectNeedsSync = true;

                CanvasView.InvalidateSurface();
                ImageLoaded?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    private void BakeDrawingsIntoImage()
    {
        if (_strokes.Count == 0 && _arrows.Count == 0)
            return;

        if (InteractionMode is not SkiaPhotoEditorInteractionMode.Draw
            and not SkiaPhotoEditorInteractionMode.Arrow)
            PushEditHistory();

        FlattenOverlaysIntoBitmap();
        CanvasView.InvalidateSurface();
    }

    private void BakeTextsIntoImage()
    {
        if (_textOverlays.Count == 0)
            return;

        if (InteractionMode != SkiaPhotoEditorInteractionMode.Text)
            PushEditHistory();

        FlattenOverlaysIntoBitmap();
        CanvasView.InvalidateSurface();
    }

    private static void OnDrawStrokeAppearancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SkiaPhotoEditorView editor)
            return;

        if (editor._activeStroke is not null)
        {
            editor._activeStroke.Color = editor.StrokeColor.ToSkColor();
            editor._activeStroke.Width = editor.ViewSizeToBitmapSize(editor.DrawStrokeWidth);
        }

        editor.RefreshVisibleTextInputHost();
    }

    private static void OnArrowStrokeWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SkiaPhotoEditorView editor)
            return;

        if (editor._activeArrow is not null)
            editor._activeArrow.Width = editor.ViewSizeToBitmapSize(editor.ArrowStrokeWidth);

        editor.CanvasView.InvalidateSurface();
    }

    private static void OnTextFontSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SkiaPhotoEditorView editor)
            editor.RefreshVisibleTextInputHost();
    }

    private void RaiseInteractionModeChanged() => InteractionModeChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseDrawingHistoryChanged() => DrawingHistoryChanged?.Invoke(this, EventArgs.Empty);

    private void OnUnloaded(object? sender, EventArgs e)
    {
        UnsubscribeThemeChanges();
        Interlocked.Increment(ref _loadGeneration);

        lock (_bitmapLock)
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }

        _strokes.Clear();
        _redoStrokes.Clear();
        _arrows.Clear();
        _redoArrows.Clear();
        _textOverlays.Clear();
        _textRedoOverlays.Clear();
        HideTextInputHost();
        ClearEditHistoryStacks();
        _activeStroke = null;
        CancelArrowHeadRevealAnimation();
        _activeArrow = null;
    }

    private static Task RunOnUiAsync(Func<Task> action) =>
        MainThread.IsMainThread ? action() : MainThread.InvokeOnMainThreadAsync(action);
}

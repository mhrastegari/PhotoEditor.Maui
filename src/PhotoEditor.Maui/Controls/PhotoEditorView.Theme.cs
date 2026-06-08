using Microsoft.Maui.Controls.Shapes;

namespace PhotoEditor.Maui;

public partial class PhotoEditorView
{
    private PhotoEditorResolvedTheme _resolvedTheme;

    private enum ToolbarButtonKind
    {
        Tool,
        ToolActive,
        Symbol,
        Primary,
        Secondary
    }

    private void SubscribeThemeChanges()
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void UnsubscribeThemeChanges()
    {
        Application.Current?.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyThemeFromOptions();

    private void ApplyThemeFromOptions()
    {
        _resolvedTheme = PhotoEditorResolvedTheme.Resolve(this, Theme);

        BackgroundColor = _resolvedTheme.BackgroundColor;

        StyleThemedShellBorder(TopToolbarShell, _resolvedTheme.ToolbarBackgroundColor);
        StyleThemedShellBorder(DrawColorPaletteShell, _resolvedTheme.PaletteBackgroundColor);
        StyleThemedShellBorder(TextFontSizePaletteShell, _resolvedTheme.PaletteBackgroundColor);

        PhotoEditorControl.ApplyCanvasThemeFromOptions();
        ApplyToolbarLabels();
    }

    private void StyleThemedShellBorder(Border border, Color backgroundColor)
    {
        border.BackgroundColor = backgroundColor;
        border.Stroke = _resolvedTheme.ButtonBorderColor;
        border.StrokeThickness = _resolvedTheme.ButtonBorderWidth;
        border.StrokeShape = new RoundRectangle { CornerRadius = _resolvedTheme.ToolbarCornerRadius };
    }

    private void RefreshToolbarButtons()
    {
        var mode = PhotoEditorControl.InteractionMode;
        var isCrop = mode == SkiaPhotoEditorInteractionMode.Crop;
        var isDraw = mode == SkiaPhotoEditorInteractionMode.Draw;
        var isArrow = mode == SkiaPhotoEditorInteractionMode.Arrow;
        var isText = mode == SkiaPhotoEditorInteractionMode.Text;

        StyleToolbarButton(UndoToolButton, ToolbarButtonKind.Symbol);
        StyleToolbarButton(RedoToolButton, ToolbarButtonKind.Symbol);
        StyleToolbarButton(CropToolButton, isCrop ? ToolbarButtonKind.ToolActive : ToolbarButtonKind.Tool);
        StyleToolbarButton(DrawToolButton, isDraw ? ToolbarButtonKind.ToolActive : ToolbarButtonKind.Tool);
        StyleToolbarButton(ArrowToolButton, isArrow ? ToolbarButtonKind.ToolActive : ToolbarButtonKind.Tool);
        StyleToolbarButton(TextToolButton, isText ? ToolbarButtonKind.ToolActive : ToolbarButtonKind.Tool);
        StyleToolbarButton(RotateCropToolButton, isCrop ? ToolbarButtonKind.ToolActive : ToolbarButtonKind.Symbol);
        StyleToolbarButton(CancelCropToolButton, ToolbarButtonKind.Secondary);
        StyleToolbarButton(ApplyCropToolButton, ToolbarButtonKind.Primary);
        StyleToolbarButton(CancelButton, ToolbarButtonKind.Secondary);
        StyleToolbarButton(DoneButton, ToolbarButtonKind.Primary);

        ApplyDisabledButtonStyle(UndoToolButton);
        ApplyDisabledButtonStyle(RedoToolButton);
    }

    private void StyleToolbarButton(Button button, ToolbarButtonKind kind)
    {
        var radius = (int)Math.Round(_resolvedTheme.ButtonCornerRadius);
        var isIcon = button.ImageSource is FontImageSource;
        button.CornerRadius = radius;
        button.HeightRequest = 44;
        button.FontAttributes = FontAttributes.Bold;
        button.BorderWidth = _resolvedTheme.ButtonBorderWidth;

        switch (kind)
        {
            case ToolbarButtonKind.Tool:
                button.BackgroundColor = _resolvedTheme.SecondaryButtonBackgroundColor;
                button.TextColor = _resolvedTheme.TextSecondaryColor;
                button.BorderColor = _resolvedTheme.ButtonBorderColor;
                button.MinimumWidthRequest = UsesCompactToolbarWidth(button) ? 44 : 64;
                break;
            case ToolbarButtonKind.ToolActive:
                button.BackgroundColor = _resolvedTheme.ActiveToolBackgroundColor;
                button.TextColor = _resolvedTheme.ActiveToolBorderColor;
                button.BorderColor = _resolvedTheme.ActiveToolBorderColor;
                button.MinimumWidthRequest = UsesCompactToolbarWidth(button) ? 44 : 64;
                break;
            case ToolbarButtonKind.Symbol:
                button.BackgroundColor = _resolvedTheme.SecondaryButtonBackgroundColor;
                button.TextColor = _resolvedTheme.TextSecondaryColor;
                button.BorderColor = _resolvedTheme.ButtonBorderColor;
                button.WidthRequest = 44;
                button.MinimumWidthRequest = 44;
                button.Padding = new Thickness(0);
                if (!isIcon && button.FontSize < 18)
                    button.FontSize = 20;
                break;
            case ToolbarButtonKind.Primary:
                button.BackgroundColor = _resolvedTheme.AccentColor;
                button.TextColor = _resolvedTheme.DoneButtonTextColor;
                button.BorderColor = Colors.Transparent;
                button.BorderWidth = 0;
                button.Padding = isIcon ? new Thickness(0) : new Thickness(16, 0);
                button.MinimumWidthRequest = isIcon ? 44 : button.MinimumWidthRequest;
                if (!isIcon && button.FontSize < 15)
                    button.FontSize = 15;
                break;
            case ToolbarButtonKind.Secondary:
                button.BackgroundColor = _resolvedTheme.SecondaryButtonBackgroundColor;
                button.TextColor = _resolvedTheme.TextPrimaryColor;
                button.BorderColor = _resolvedTheme.ButtonBorderColor;
                button.Padding = isIcon ? new Thickness(0) : new Thickness(16, 0);
                button.MinimumWidthRequest = isIcon ? 44 : button.MinimumWidthRequest;
                if (!isIcon && button.FontSize < 15)
                    button.FontSize = 15;
                break;
        }

        ApplyIconTint(button, kind);
    }

    private void ApplyIconTint(Button button, ToolbarButtonKind kind)
    {
        if (button.ImageSource is not FontImageSource icon)
            return;

        icon.Color = kind switch
        {
            ToolbarButtonKind.ToolActive => _resolvedTheme.ActiveToolBorderColor,
            ToolbarButtonKind.Primary => _resolvedTheme.DoneButtonTextColor,
            ToolbarButtonKind.Secondary => _resolvedTheme.TextPrimaryColor,
            _ => _resolvedTheme.TextSecondaryColor,
        };
    }

    private static bool UsesCompactToolbarWidth(Button button) =>
        button.ImageSource is FontImageSource
        || string.IsNullOrEmpty(button.Text)
        || button.Text.Length <= 2;

    private static void ApplyDisabledButtonStyle(Button button)
    {
        button.Opacity = button.IsEnabled ? 1 : 0.4;
    }
}

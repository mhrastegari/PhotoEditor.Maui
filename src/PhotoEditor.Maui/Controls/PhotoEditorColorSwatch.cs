using Microsoft.Maui.Controls.Shapes;

namespace PhotoEditor.Maui;

/// <summary>
/// Palette swatches with a neutral edge and shadow.
/// </summary>
public static class PhotoEditorColorSwatch
{
    public static Color DefaultRingColor { get; } = Color.FromArgb("#4D808080");

    private static Color DefaultShadowColor { get; } = Color.FromArgb("#66808080");

    public static Border Create(Color fill, double size = 32, double cornerRadius = 16) =>
        CreateCore(fill, size, cornerRadius);

    public static void ApplyNeutralEdge(Border border)
    {
        border.Stroke = DefaultRingColor;
        border.StrokeThickness = 1;
        border.Shadow = CreateShadow();
    }

    public static void SetSelected(Border border, bool selected, Color accentColor)
    {
        border.Stroke = selected ? accentColor : DefaultRingColor;
        border.StrokeThickness = selected ? 2 : 1;
    }

    private static Border CreateCore(Color fill, double size, double cornerRadius)
    {
        var border = new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Padding = 0,
            BackgroundColor = fill,
            StrokeShape = new RoundRectangle { CornerRadius = cornerRadius },
        };

        ApplyNeutralEdge(border);
        return border;
    }

    private static Shadow CreateShadow() => new()
    {
        Brush = new SolidColorBrush(DefaultShadowColor),
        Offset = new Point(0, 1),
        Radius = 2,
        Opacity = 1,
    };
}

using SkiaSharp;

namespace PhotoEditor.Maui;

internal sealed class CroppingRectangle
{
    private const float MinimumEdgePx = 10f;

    private readonly SKRect _maxRect;

    public CroppingRectangle(SKRect maxRect)
    {
        _maxRect = maxRect;
        Rect = maxRect;
    }

    public SKRect Rect { get; set; }

    public void ClampPositionToBounds()
    {
        var rect = Rect;
        var width = rect.Width;
        var height = rect.Height;

        rect.Left = Math.Clamp(rect.Left, _maxRect.Left, _maxRect.Right - width);
        rect.Top = Math.Clamp(rect.Top, _maxRect.Top, _maxRect.Bottom - height);
        rect.Right = rect.Left + width;
        rect.Bottom = rect.Top + height;
        Rect = rect;
    }

    public SKRect MaxBounds => _maxRect;

    public SKPoint[] Corners =>
    [
        new(Rect.Left, Rect.Top),
        new(Rect.Right, Rect.Top),
        new(Rect.Right, Rect.Bottom),
        new(Rect.Left, Rect.Bottom)
    ];

    public int HitTestCorner(SKPoint point, float radius)
    {
        var corners = Corners;
        for (var index = 0; index < corners.Length; index++)
        {
            var diff = point - corners[index];
            if (diff.LengthSquared < radius * radius)
                return index;
        }

        return -1;
    }

    public void MoveCorner(int index, SKPoint point)
    {
        var rect = Rect;

        switch (index)
        {
            case 0:
                rect.Left = Math.Min(Math.Max(point.X, _maxRect.Left), rect.Right - MinimumEdgePx);
                rect.Top = Math.Min(Math.Max(point.Y, _maxRect.Top), rect.Bottom - MinimumEdgePx);
                break;
            case 1:
                rect.Right = Math.Max(Math.Min(point.X, _maxRect.Right), rect.Left + MinimumEdgePx);
                rect.Top = Math.Min(Math.Max(point.Y, _maxRect.Top), rect.Bottom - MinimumEdgePx);
                break;
            case 2:
                rect.Right = Math.Max(Math.Min(point.X, _maxRect.Right), rect.Left + MinimumEdgePx);
                rect.Bottom = Math.Max(Math.Min(point.Y, _maxRect.Bottom), rect.Top + MinimumEdgePx);
                break;
            case 3:
                rect.Left = Math.Min(Math.Max(point.X, _maxRect.Left), rect.Right - MinimumEdgePx);
                rect.Bottom = Math.Max(Math.Min(point.Y, _maxRect.Bottom), rect.Top + MinimumEdgePx);
                break;
        }

        Rect = rect;
    }

    public bool Contains(SKPoint point) => Rect.Contains(point);

    public bool ContainsWithTolerance(SKPoint point, float tolerance)
    {
        var rect = Rect;
        rect.Inflate(tolerance, tolerance);
        return rect.Contains(point);
    }

    public int HitTestEdge(SKPoint point, float tolerance)
    {
        var rect = Rect;

        if (Math.Abs(point.Y - rect.Top) <= tolerance &&
            point.X >= rect.Left - tolerance && point.X <= rect.Right + tolerance)
            return 0;

        if (Math.Abs(point.Y - rect.Bottom) <= tolerance &&
            point.X >= rect.Left - tolerance && point.X <= rect.Right + tolerance)
            return 1;

        if (Math.Abs(point.X - rect.Left) <= tolerance &&
            point.Y >= rect.Top - tolerance && point.Y <= rect.Bottom + tolerance)
            return 2;

        if (Math.Abs(point.X - rect.Right) <= tolerance &&
            point.Y >= rect.Top - tolerance && point.Y <= rect.Bottom + tolerance)
            return 3;

        return -1;
    }
}

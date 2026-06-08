using SkiaSharp;

namespace PhotoEditor.Maui;

internal static class SkiaPhotoEditorArrowRenderer
{
    private const float HeadAngleRadians = 0.58f;
    private const float MinHeadLength = 12f;
    private const float MaxHeadLength = 54f;
    private const float HeadSizeMultiplier = 3f;
    private const float MinDirectionSegmentLength = 0.5f;
    private const int DirectionTailPointCount = 5;

    public static void DrawPath(
        SKCanvas canvas,
        IReadOnlyList<SKPoint> points,
        SKColor color,
        float strokeWidth)
    {
        if (points.Count < 2)
            return;

        using var strokePaint = new SKPaint
        {
            Color = color,
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        using var path = new SKPath();
        path.MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++)
            path.LineTo(points[i]);

        canvas.DrawPath(path, strokePaint);
    }

    public static void DrawPathWithArrowHead(
        SKCanvas canvas,
        IReadOnlyList<SKPoint> points,
        SKColor color,
        float strokeWidth,
        float headRevealProgress = 1f)
    {
        if (headRevealProgress <= 0.001f)
        {
            DrawPath(canvas, points, color, strokeWidth);
            return;
        }

        if (!TryGetEndDirection(points, out var tip, out var angle))
        {
            DrawPath(canvas, points, color, strokeWidth);
            return;
        }

        var headSpan = ComputeHeadSpan(strokeWidth, headRevealProgress);
        if (headSpan < 0.5f)
        {
            DrawPath(canvas, points, color, strokeWidth);
            return;
        }

        var layoutHeadSpan = ComputeHeadSpan(strokeWidth, 1f);
        ComputeChevronWings(tip, angle, layoutHeadSpan, out var left, out var right);
        var baseCenter = new SKPoint(
            (left.X + right.X) * 0.5f,
            (left.Y + right.Y) * 0.5f);

        var headDepth = Distance(tip, baseCenter);
        DrawShaft(canvas, points, tip, baseCenter, headDepth, color, strokeWidth);
        DrawChevronHead(canvas, tip, angle, color, strokeWidth, headRevealProgress);
    }

    private static void DrawChevronHead(
        SKCanvas canvas,
        SKPoint tip,
        float angle,
        SKColor color,
        float strokeWidth,
        float headRevealProgress)
    {
        var progress = Math.Clamp(headRevealProgress, 0f, 1f);
        var headSpan = ComputeHeadSpan(strokeWidth, progress);
        if (headSpan < 0.5f)
            return;

        ComputeChevronWings(tip, angle, headSpan, out var left, out var right);
        var leftWing = Lerp(tip, left, progress);
        var rightWing = Lerp(tip, right, progress);

        using var chevronPath = new SKPath();
        chevronPath.MoveTo(leftWing);
        chevronPath.LineTo(tip);
        chevronPath.LineTo(rightWing);

        using var strokePaint = new SKPaint
        {
            Color = color.WithAlpha((byte)(color.Alpha * progress)),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        canvas.DrawPath(chevronPath, strokePaint);
    }

    private static void DrawShaft(
        SKCanvas canvas,
        IReadOnlyList<SKPoint> points,
        SKPoint tip,
        SKPoint baseCenter,
        float headDepth,
        SKColor color,
        float strokeWidth)
    {
        if (points.Count < 2)
            return;

        var shaftEnd = Lerp(baseCenter, tip, 0.88f);
        var tailTrimDistance = Math.Min(headDepth * 0.35f, strokeWidth * 2f);

        using var path = new SKPath();
        if (TryTrimPolylineFromTip(points, tailTrimDistance, out _, out var lastKeptPointIndex))
        {
            path.MoveTo(points[0]);
            for (var i = 1; i <= lastKeptPointIndex; i++)
                path.LineTo(points[i]);

            var anchor = lastKeptPointIndex >= 0 ? points[lastKeptPointIndex] : points[0];
            if (Distance(anchor, shaftEnd) > 0.5f)
                path.LineTo(shaftEnd);
        }
        else
        {
            path.MoveTo(points[0]);
            for (var i = 1; i < points.Count; i++)
                path.LineTo(points[i]);

            if (Distance(points[^1], shaftEnd) > 0.5f)
                path.LineTo(shaftEnd);
        }

        using var strokePaint = new SKPaint
        {
            Color = color,
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        canvas.DrawPath(path, strokePaint);

        using var startCapPaint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(points[0], strokeWidth * 0.5f, startCapPaint);
    }

    /// <summary>
    /// Walks backward from the polyline tip and trims only the tail — keeps loops and curves intact.
    /// </summary>
    private static bool TryTrimPolylineFromTip(
        IReadOnlyList<SKPoint> points,
        float trimDistance,
        out SKPoint shaftEnd,
        out int lastKeptPointIndex)
    {
        shaftEnd = default;
        lastKeptPointIndex = points.Count - 1;

        if (points.Count < 2 || trimDistance <= 0f)
            return false;

        var polylineLength = GetPolylineLength(points);
        if (polylineLength < MinDirectionSegmentLength)
            return false;

        trimDistance = Math.Min(trimDistance, Math.Max(0f, polylineLength - MinDirectionSegmentLength));

        var remaining = trimDistance;
        for (var i = points.Count - 1; i >= 1; i--)
        {
            var segmentStart = points[i - 1];
            var segmentEnd = points[i];
            var segmentLength = Distance(segmentStart, segmentEnd);
            if (segmentLength < MinDirectionSegmentLength)
                continue;

            if (remaining <= segmentLength)
            {
                var t = 1f - remaining / segmentLength;
                shaftEnd = new SKPoint(
                    segmentStart.X + (segmentEnd.X - segmentStart.X) * t,
                    segmentStart.Y + (segmentEnd.Y - segmentStart.Y) * t);
                lastKeptPointIndex = i - 1;
                return true;
            }

            remaining -= segmentLength;
        }

        shaftEnd = points[0];
        lastKeptPointIndex = -1;
        return true;
    }

    private static float GetPolylineLength(IReadOnlyList<SKPoint> points)
    {
        var length = 0f;
        for (var i = 1; i < points.Count; i++)
            length += Distance(points[i - 1], points[i]);

        return length;
    }

    private static float ComputeHeadSpan(float strokeWidth, float progress) =>
        Math.Clamp(strokeWidth * 3.5f * HeadSizeMultiplier, MinHeadLength, MaxHeadLength)
        * Math.Clamp(progress, 0f, 1f);

    private static void ComputeChevronWings(
        SKPoint tip,
        float angle,
        float headSpan,
        out SKPoint left,
        out SKPoint right)
    {
        left = new SKPoint(
            tip.X - headSpan * MathF.Cos(angle - HeadAngleRadians),
            tip.Y - headSpan * MathF.Sin(angle - HeadAngleRadians));
        right = new SKPoint(
            tip.X - headSpan * MathF.Cos(angle + HeadAngleRadians),
            tip.Y - headSpan * MathF.Sin(angle + HeadAngleRadians));
    }

    private static SKPoint Lerp(SKPoint from, SKPoint to, float t) =>
        new(
            from.X + (to.X - from.X) * t,
            from.Y + (to.Y - from.Y) * t);

    private static float Distance(SKPoint a, SKPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static bool TryGetEndDirection(
        IReadOnlyList<SKPoint> points,
        out SKPoint tip,
        out float angle)
    {
        tip = points[^1];
        if (points.Count < 2)
        {
            angle = 0;
            return false;
        }

        var tailStart = Math.Max(0, points.Count - DirectionTailPointCount);
        var sumDx = 0f;
        var sumDy = 0f;
        var segmentCount = 0;

        for (var i = tailStart + 1; i < points.Count; i++)
        {
            var dx = points[i].X - points[i - 1].X;
            var dy = points[i].Y - points[i - 1].Y;
            var length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < MinDirectionSegmentLength)
                continue;

            sumDx += dx / length;
            sumDy += dy / length;
            segmentCount++;
        }

        if (segmentCount > 0)
        {
            angle = MathF.Atan2(sumDy, sumDx);
            return true;
        }

        var fallbackDx = tip.X - points[tailStart].X;
        var fallbackDy = tip.Y - points[tailStart].Y;
        var fallbackLength = MathF.Sqrt(fallbackDx * fallbackDx + fallbackDy * fallbackDy);
        if (fallbackLength < MinDirectionSegmentLength)
        {
            angle = 0;
            return false;
        }

        angle = MathF.Atan2(fallbackDy, fallbackDx);
        return true;
    }
}

using SkiaSharp;

namespace PhotoEditor.Maui;

internal static class SkiaPhotoEditorBitmapHelper
{
    public static SKBitmap? DecodeFromFile(string imagePath, double maxEditWidth = 0, double maxEditHeight = 0)
    {
        if (!File.Exists(imagePath))
            return null;

        using var stream = File.OpenRead(imagePath);
        using var codec = SKCodec.Create(stream);
        if (codec is null)
            return null;

        var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
            return null;

        var oriented = ApplyEncodedOrigin(bitmap, codec.EncodedOrigin);
        var normalized = NormalizeBitmap(oriented);

        if (maxEditWidth > 0 && maxEditHeight > 0)
            return ResizeToFit(normalized, maxEditWidth, maxEditHeight);

        return normalized;
    }

    public static SKBitmap ResizeToFit(SKBitmap source, double maxWidth, double maxHeight)
    {
        if (source.IsNull || source.Width <= 0 || source.Height <= 0)
            return source;

        var ratio = Math.Min(maxWidth / source.Width, maxHeight / source.Height);
        if (ratio >= 1)
            return source.Copy();

        var targetWidth = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var targetHeight = Math.Max(1, (int)Math.Round(source.Height * ratio));

        var info = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        var resized = source.Resize(info, SKSamplingOptions.Default) ?? source.Copy();
        if (!ReferenceEquals(resized, source))
            source.Dispose();

        return NormalizeBitmap(resized);
    }

    public static SKBitmap NormalizeBitmap(SKBitmap source)
    {
        if (source.ColorType == SKColorType.Rgba8888 && source.AlphaType is SKAlphaType.Premul or SKAlphaType.Unpremul)
            return source;

        var normalized = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(normalized);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, 0, 0);
        source.Dispose();
        return normalized;
    }

    public static SKBitmap? CropBitmapFromRect(SKBitmap source, SKRect cropRect)
    {
        var width = (int)cropRect.Width;
        var height = (int)cropRect.Height;
        if (width <= 0 || height <= 0)
            return null;

        var cropped = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(cropped);
        canvas.Clear(SKColors.White);

        var sourceRect = new SKRect(cropRect.Left, cropRect.Top, cropRect.Right, cropRect.Bottom);
        var destRect = new SKRect(0, 0, cropRect.Width, cropRect.Height);
        canvas.DrawBitmap(source, sourceRect, destRect);
        return NormalizeBitmap(cropped);
    }

    public static SKBitmap RotateBitmap(SKBitmap source, int degreesClockwise)
    {
        var degrees = ((degreesClockwise % 360) + 360) % 360;
        if (degrees == 0)
            return source;

        SKBitmap rotated;
        switch (degrees)
        {
            case 90:
                rotated = new SKBitmap(source.Height, source.Width);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                    canvas.DrawBitmap(source, 0, 0);
                }
                break;
            case 180:
                rotated = new SKBitmap(source.Width, source.Height);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.Translate(source.Width, source.Height);
                    canvas.RotateDegrees(180);
                    canvas.DrawBitmap(source, 0, 0);
                }
                break;
            case 270:
                rotated = new SKBitmap(source.Height, source.Width);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(270);
                    canvas.DrawBitmap(source, 0, 0);
                }
                break;
            default:
                return source;
        }

        source.Dispose();
        return NormalizeBitmap(rotated);
    }

    private static SKBitmap ApplyEncodedOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return bitmap;

        var swapsDimensions = origin >= SKEncodedOrigin.LeftTop;
        var destWidth = swapsDimensions ? bitmap.Height : bitmap.Width;
        var destHeight = swapsDimensions ? bitmap.Width : bitmap.Height;

        var oriented = new SKBitmap(destWidth, destHeight, bitmap.ColorType, bitmap.AlphaType);
        using (var canvas = new SKCanvas(oriented))
        {
            canvas.Clear(SKColors.White);
            canvas.SetMatrix(CreateEncodedOriginMatrix(origin, destWidth, destHeight));
            canvas.DrawBitmap(bitmap, 0, 0);
        }

        bitmap.Dispose();
        return oriented;
    }

    private static SKMatrix CreateEncodedOriginMatrix(SKEncodedOrigin origin, int width, int height) =>
        origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, width, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, height, 0, 0, 1),
            _ => SKMatrix.CreateIdentity(),
        };
}

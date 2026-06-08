using PhotoEditor.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace PhotoEditor.Maui;

public partial class SkiaPhotoEditorView
{
    public const int DefaultOutputQuality = 92;

    public static readonly BindableProperty OutputFormatProperty = BindableProperty.Create(
        nameof(OutputFormat),
        typeof(PhotoEditorOutputFormat),
        typeof(SkiaPhotoEditorView),
        PhotoEditorOutputFormat.Png);

    public static readonly BindableProperty OutputQualityProperty = BindableProperty.Create(
        nameof(OutputQuality),
        typeof(int),
        typeof(SkiaPhotoEditorView),
        DefaultOutputQuality,
        validateValue: (_, value) => value is int quality && quality is >= 1 and <= 100);

    public PhotoEditorOutputFormat OutputFormat
    {
        get => (PhotoEditorOutputFormat)GetValue(OutputFormatProperty);
        set => SetValue(OutputFormatProperty, value);
    }

    public int OutputQuality
    {
        get => (int)GetValue(OutputQualityProperty);
        set => SetValue(OutputQualityProperty, value);
    }

    public Task<MemoryStream?> GetEditedImageStreamAsync(
        double maxOutputWidth,
        double maxOutputHeight,
        CancellationToken cancellationToken = default) =>
        ExportEditedImageAsync(maxOutputWidth, maxOutputHeight, OutputFormat, OutputQuality, cancellationToken);

    public Task<MemoryStream?> GetEditedPngStreamAsync(
        double maxOutputWidth,
        double maxOutputHeight,
        CancellationToken cancellationToken = default) =>
        ExportEditedImageAsync(maxOutputWidth, maxOutputHeight, PhotoEditorOutputFormat.Png, 100, cancellationToken);

    public Task<string?> SaveEditedImageAsync(
        string directory,
        string fileNameWithoutExtension,
        double maxOutputWidth,
        double maxOutputHeight,
        CancellationToken cancellationToken = default) =>
        SaveEditedImageAsync(
            directory,
            fileNameWithoutExtension,
            maxOutputWidth,
            maxOutputHeight,
            OutputFormat,
            OutputQuality,
            cancellationToken);

    public Task<string?> SaveEditedPngAsync(
        string directory,
        string fileNameWithoutExtension,
        double maxOutputWidth,
        double maxOutputHeight,
        CancellationToken cancellationToken = default) =>
        SaveEditedImageAsync(
            directory,
            fileNameWithoutExtension,
            maxOutputWidth,
            maxOutputHeight,
            PhotoEditorOutputFormat.Png,
            100,
            cancellationToken);

    private async Task<MemoryStream?> ExportEditedImageAsync(
        double maxOutputWidth,
        double maxOutputHeight,
        PhotoEditorOutputFormat outputFormat,
        int outputQuality,
        CancellationToken cancellationToken)
    {
        if (!IsImageLoaded)
            return null;

        SKBitmap? composite = null;
        _isMutatingBitmap = true;
        try
        {
            await RunOnUiAsync(async () =>
            {
                FlattenOverlaysIntoBitmap();

                lock (_bitmapLock)
                    composite = _bitmap?.Copy();

                CanvasView.InvalidateSurface();
                await Task.CompletedTask.ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        finally
        {
            _isMutatingBitmap = false;
        }

        if (composite is null || composite.IsNull)
            return null;

        try
        {
            return await Task.Run(
                    () => EncodeImageStream(composite, maxOutputWidth, maxOutputHeight, outputFormat, outputQuality, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            composite.Dispose();
        }
    }

    private static MemoryStream? EncodeImageStream(
        SKBitmap composite,
        double maxOutputWidth,
        double maxOutputHeight,
        PhotoEditorOutputFormat outputFormat,
        int outputQuality,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var resized = SkiaPhotoEditorBitmapHelper.ResizeToFit(composite, maxOutputWidth, maxOutputHeight);
        if (resized.IsNull)
            return null;

        using var image = SKImage.FromBitmap(resized);
        if (image is null)
            return null;

        var skFormat = outputFormat == PhotoEditorOutputFormat.Jpeg
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;
        var quality = skFormat == SKEncodedImageFormat.Jpeg ? Math.Clamp(outputQuality, 1, 100) : 100;

        using var data = image.Encode(skFormat, quality);
        if (data is null)
            return null;

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task<string?> SaveEditedImageAsync(
        string directory,
        string fileNameWithoutExtension,
        double maxOutputWidth,
        double maxOutputHeight,
        PhotoEditorOutputFormat outputFormat,
        int outputQuality,
        CancellationToken cancellationToken)
    {
        await using var stream = await ExportEditedImageAsync(
                maxOutputWidth,
                maxOutputHeight,
                outputFormat,
                outputQuality,
                cancellationToken)
            .ConfigureAwait(false);

        if (stream is null || stream.Length == 0)
            return null;

        Directory.CreateDirectory(directory);
        var extension = outputFormat == PhotoEditorOutputFormat.Jpeg ? ".jpg" : ".png";
        var path = Path.Combine(directory, $"{fileNameWithoutExtension}{extension}");
        await using var file = File.Create(path);
        stream.Position = 0;
        await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        return File.Exists(path) && new FileInfo(path).Length > 0 ? path : null;
    }
}

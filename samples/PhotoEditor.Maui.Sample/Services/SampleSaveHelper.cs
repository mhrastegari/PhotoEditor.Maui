using CommunityToolkit.Maui.Storage;

namespace PhotoEditor.Maui.Sample.Services;

internal static class SampleSaveHelper
{
    public static async Task<string?> SaveWithPickerAsync(PhotoEditorView editor)
    {
        if (!editor.PhotoEditor.IsImageLoaded)
            return null;

        var fileName = CreateEditedFileName(editor.ImageSourcePath, editor.OutputFormat);
        await using var stream = await editor.GetEditedImageStreamAsync().ConfigureAwait(true);
        if (stream is null)
            return null;

        var result = await FileSaver.Default.SaveAsync(fileName, stream).ConfigureAwait(true);
        return result.IsSuccessful ? result.FilePath : null;
    }

    public static string CreateEditedFileNameWithoutExtension(string? sourceImagePath)
    {
        if (!string.IsNullOrWhiteSpace(sourceImagePath))
            return $"{Path.GetFileNameWithoutExtension(sourceImagePath)}_edited";

        return $"edited_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    public static string CreateEditedFileName(string? sourceImagePath, PhotoEditorOutputFormat format)
    {
        var extension = format == PhotoEditorOutputFormat.Jpeg ? ".jpg" : ".png";
        return CreateEditedFileNameWithoutExtension(sourceImagePath) + extension;
    }
}

using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PhotoEditor.Maui;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UsePhotoEditor(
        this MauiAppBuilder builder,
        Action<PhotoEditorOptions>? configure = null)
    {
        builder.UseSkiaSharp();
        configure?.Invoke(PhotoEditorOptions.Default);
        return builder;
    }
}
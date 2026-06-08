namespace PhotoEditor.Maui;

/// <summary>
/// Skia text rendering fonts loaded from the app package via <see cref="FileSystem.OpenAppPackageFileAsync"/>.
/// Register assets in the host app and optionally call <see cref="MauiAppBuilderExtensions.UsePhotoEditor"/>.
/// </summary>
public sealed class PhotoEditorTextFontOptions
{
    public static PhotoEditorTextFontOptions Current { get; set; } = new();

    /// <summary>App-package file name for Latin text. Null uses the platform default Skia typeface.</summary>
    public string? LatinFontAssetFileName { get; set; }

    /// <summary>Optional app-package file name for RTL scripts (e.g. Arabic/Persian).</summary>
    public string? RtlFontAssetFileName { get; set; }

    /// <summary>When text is empty, use this to pick RTL vs Latin typeface.</summary>
    public Func<bool>? PreferRtlTypeface { get; set; }
}

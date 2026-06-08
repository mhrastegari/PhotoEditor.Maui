# PhotoEditor.Maui

A cross-platform .NET MAUI photo editor control with crop, freehand draw, arrows, and text overlays. Built on SkiaSharp with an optional full UI shell (`PhotoEditorView`) or a bare canvas (`SkiaPhotoEditorView`).

## Features

- Crop with rotate
- Freehand draw with color palette
- Arrow annotations
- Text overlays (RTL-aware when configured)
- Undo / Redo
- PNG or JPEG export with max dimensions and quality
- Themeable built-in toolbar (text, Unicode symbols, or icon font)

## Installation

```bash
dotnet add package PhotoEditor.Maui
```

## Setup

In `MauiProgram.cs`:

```csharp
builder
    .UseMauiApp<App>()
    .UsePhotoEditor();
```

`UsePhotoEditor()` registers SkiaSharp handlers (required for `SKCanvasView` on Windows and other platforms). The library does not register or bundle any fonts.

**Text fonts** — overlay text uses the platform default Skia typeface unless you point `PhotoEditorTextFontOptions.Current` at app-package font files:

```csharp
builder.UsePhotoEditor();
PhotoEditorTextFontOptions.Current.LatinFontAssetFileName = "MyFont-Regular.ttf";
PhotoEditorTextFontOptions.Current.RtlFontAssetFileName = "MyFont-Arabic.ttf";
```

Pack fonts as `MauiAsset` (with `LogicalName`) so `FileSystem.OpenAppPackageFileAsync` can load them at runtime. Configure RTL picking via `PreferRtlTypeface`.

The built-in toolbar uses text labels and Unicode symbols by default (no icon font).

### Customization

Configure defaults app-wide in `MauiProgram.cs`. Leave theme properties unset (`null`) to inherit your app resource dictionary and light/dark theme:

```csharp
builder.UsePhotoEditor(options =>
{
    // Optional — only set what you want to override
    options.Theme.AccentColor = Color.FromArgb("#E91E63");
    options.Canvas.DrawColors = [Colors.White, Colors.Black, Colors.Red];
    options.Canvas.DefaultStrokeWidth = 8f;
    options.Canvas.Arrow.DefaultStrokeWidth = 10f;
    options.Features.Arrow = false; // hide arrow tool in built-in toolbar
    options.Messages.DiscardTitle = "Leave editor?";
});
```

Unset theme colors resolve from app resources (`Primary`, `Secondary`, `Gray*`, etc.) with MAUI template fallbacks, and update automatically when `Application.Current.RequestedTheme` changes.

For a single editor instance, clone defaults so you do not mutate the shared `PhotoEditorOptions.Default`:

```csharp
editor.Options = PhotoEditorOptions.Default.Clone();
```

**Toolbar icons** — set `Theme.Toolbar.IconFontFamily` to a registered icon font and override individual glyphs (defaults are text labels, not icon code points):

```csharp
builder.UsePhotoEditor(options =>
{
    options.Theme.Toolbar.IconFontFamily = "MaterialIcons";
    options.Theme.Toolbar.Undo = "\ue166";
    options.Theme.Toolbar.Crop = "\ue3be";
    options.Theme.Toolbar.Done = "\ue876";
});
```

Register the font in your app's `MauiProgram.cs` (`fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons")`). See `samples/PhotoEditor.Maui.Sample/Themes/MaterialIconsToolbar.cs` for a full glyph set.

Or per control in XAML/code via the `Options` property on `PhotoEditorView` or `SkiaPhotoEditorView`.

`PhotoEditorOptions` groups:

- **Canvas** — draw palette, text sizes, draw/arrow stroke width defaults and ranges, undo depth, max edit dimensions (`MaxEditWidth` / `MaxEditHeight` downscale on load), crop/arrow/text-input tuning
- **Theme** — optional accent/surface/text colors (null = app theme), toolbar and palette surfaces, button borders and corner radius, active tool styling, loading overlay, tool-mode canvas margin, **Toolbar** labels/icons
- **Features** — enable/disable crop, draw, arrow, text, undo/redo
- **Messages** — discard confirmation dialog strings (defaults: "Discard changes?", "Your edits will be lost.", …)

Set `ShowBuiltInToolbar="False"` on `PhotoEditorView` and bind to `PhotoEditor` for a fully custom UI while reusing the same options. Built-in Cancel/Done and tool action bars are hidden when the toolbar is off.

Color palette swatches in the built-in UI use a neutral gray ring and shadow so every swatch stays visible on light and dark fills. For custom palettes, use `PhotoEditorColorSwatch.Create()` and `PhotoEditorColorSwatch.SetSelected()`.

### Sample app

The included sample (`samples/PhotoEditor.Maui.Sample`) launches from a home page with three options under `Pages/`:

- **Built-in UI** — `Pages/BuiltInEditorPage` (default text/Unicode toolbar, app theme colors)
- **Themed default UI** — `Pages/ThemedDefaultEditorPage` (built-in UI customized via `PhotoEditorThemeOptions`: colors, rounded buttons, Material Icons toolbar)
- **Custom UI** — `Pages/CustomEditorPage` (fully custom UI, `ShowBuiltInToolbar="False"`)

On **Done** / **Save**, the sample uses [Community Toolkit](https://learn.microsoft.com/dotnet/communitytoolkit/maui/) `FileSaver` so the user picks file name and location (`Services/SampleSaveHelper` + `PhotoEditorView.SaveImageAsync`). The library itself does not depend on Community Toolkit.

## Usage

### Full editor (toolbar + Cancel/Done)

```xml
<ContentPage xmlns:mhr="http://mhrastegari.com/photoeditor">
    <mhr:PhotoEditorView
        ImageSourcePath="{Binding PhotoPath}"
        MaxOutputHeight="720"
        MaxOutputWidth="1280"
        OutputFormat="Jpeg"
        OutputQuality="90" />
</ContentPage>
```

```csharp
var editor = new PhotoEditorView("/path/to/photo.jpg");
editor.Completed += (_, path) => { /* saved path, or null if cancelled */ };
```

Without `SaveImageAsync`, Done saves to `FileSystem.CacheDirectory` and returns that path via `Completed`.

### Skia surface only (custom UI)

```xml
<mhr:SkiaPhotoEditorView x:Name="Editor" />
```

```csharp
await Editor.LoadImageAsync(path);
Editor.OutputFormat = PhotoEditorOutputFormat.Jpeg;
Editor.OutputQuality = 90;
await Editor.SaveEditedImageAsync(FileSystem.CacheDirectory, "edited", 1280, 720);
```

**Tool orchestration** (custom toolbar):

```csharp
Editor.ToggleTool(SkiaPhotoEditorInteractionMode.Draw);  // tap again to cancel
Editor.ActivateTool(SkiaPhotoEditorInteractionMode.Crop);  // exclusive — cancels other tools
await Editor.ApplyActiveToolAsync();                       // Apply / Done prep
Editor.CancelActiveTool();
```

The same helpers are exposed on `PhotoEditorView` when using `ShowBuiltInToolbar="False"`.

## License

MIT

namespace PhotoEditor.Maui;

/// <summary>Crop overlay visuals and resize handle interaction settings.</summary>
public sealed class PhotoEditorCropOptions
{
    /// <summary>Minimum crop rectangle edge length in image pixels.</summary>
    public float MinCropSizeImagePx { get; set; } = 10f;

    /// <summary>Visible radius of crop handle dots, in view pixels.</summary>
    public float HandleRadiusViewPx { get; set; } = 10f;

    /// <summary>Corner grab half-size in view pixels; when zero, corners use <see cref="BorderHitToleranceViewPx"/>.</summary>
    public float HandleHitRadiusViewPx { get; set; } = 35f;

    /// <summary>Resize grab distance inside and outside each crop border, in view pixels.</summary>
    public float BorderHitToleranceViewPx { get; set; } = 35f;

    /// <summary>Alpha of the dimmed area outside the crop selection (0–255).</summary>
    public byte OverlayDimAlpha { get; set; } = 140;

    /// <summary>Color of the crop selection border.</summary>
    public Color OverlayBorderColor { get; set; } = Colors.White;

    /// <summary>Fill color of crop handle dots.</summary>
    public Color OverlayHandleColor { get; set; } = Colors.White;

    /// <summary>Stroke width of the crop selection border, in view pixels.</summary>
    public float OverlayBorderWidth { get; set; } = 2f;

    /// <summary>Creates a copy of these crop options.</summary>
    public PhotoEditorCropOptions Clone() => new()
    {
        MinCropSizeImagePx = MinCropSizeImagePx,
        HandleRadiusViewPx = HandleRadiusViewPx,
        HandleHitRadiusViewPx = HandleHitRadiusViewPx,
        BorderHitToleranceViewPx = BorderHitToleranceViewPx,
        OverlayDimAlpha = OverlayDimAlpha,
        OverlayBorderColor = OverlayBorderColor,
        OverlayHandleColor = OverlayHandleColor,
        OverlayBorderWidth = OverlayBorderWidth,
    };
}

namespace PhotoEditor.Maui;

/// <summary>Arrow annotation stroke limits and head reveal animation.</summary>
public sealed class PhotoEditorArrowOptions
{
    /// <summary>Minimum arrow length in view pixels required to commit a stroke.</summary>
    public float MinLengthViewPx { get; set; } = 12f;

    /// <summary>Initial arrow stroke width when arrow mode starts.</summary>
    public float DefaultStrokeWidth { get; set; } = 6f;

    /// <summary>Minimum arrow stroke width the user can select.</summary>
    public float StrokeWidthMin { get; set; } = 2f;

    /// <summary>Maximum arrow stroke width the user can select.</summary>
    public float StrokeWidthMax { get; set; } = 32f;

    /// <summary>Duration in milliseconds of the arrow-head reveal animation.</summary>
    public uint HeadRevealDurationMs { get; set; } = 220;

    /// <summary>Creates a copy of these arrow options.</summary>
    public PhotoEditorArrowOptions Clone() => new()
    {
        MinLengthViewPx = MinLengthViewPx,
        DefaultStrokeWidth = DefaultStrokeWidth,
        StrokeWidthMin = StrokeWidthMin,
        StrokeWidthMax = StrokeWidthMax,
        HeadRevealDurationMs = HeadRevealDurationMs,
    };
}

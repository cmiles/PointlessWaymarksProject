using SkiaSharp;

namespace PointlessWaymarks.UtilitarianImageCombinerGui.Controls;

public record ColorNameAndSkColor
{
    public SKColor SkiaColor { get; set; }
    public required string ColorName { get; set; }
    public required string Color { get; set; }
}
namespace PointlessWaymarks.UtilitarianImageCombinerGui.Controls;

public class WebPageAsJpegWindowImageSavedEventArgs(string newFilename) : EventArgs
{
    public string NewFilename { get; } = newFilename;
}
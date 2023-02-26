namespace PointlessWaymarks.CmsData.CommonHtml;

public class PictureFile
{
    public string? AltText { get; init; }
    public FileInfo? File { get; init; }
    public string? FileName { get; set; }
    public int Height { get; init; }
    public string? SiteUrl { get; init; }
    public int Width { get; init; }
}
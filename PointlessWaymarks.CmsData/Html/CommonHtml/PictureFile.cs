using System.IO;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public class PictureFile
    {
        public string? AltText { get; set; }
        public FileInfo? File { get; set; }
        public string? FileName { get; set; }
        public int Height { get; set; }
        public string? SiteUrl { get; set; }
        public int Width { get; set; }
    }
}
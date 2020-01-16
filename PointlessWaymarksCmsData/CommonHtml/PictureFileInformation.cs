using System.IO;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public class PictureFileInformation
    {
        public string AltText { get; set; }
        public FileInfo File { get; set; }
        public string FileName { get; set; }
        public int Height { get; set; }
        public string SiteUrl { get; set; }
        public int Width { get; set; }
    }
}
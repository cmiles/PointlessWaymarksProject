using System.IO;

namespace TheLemmonWorkshopData.TextTransforms
{
    public class ImageFileInformation
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileName { get; set; }
        public string SiteUrl { get; set; }

        public FileInfo File { get; set; }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace TheLemmonWorkshopData.PhotoHtml
{
    public class PhotoDirectoryContentsInformation
    {
        public ImageFileInformation SmallImage { get; set; }

        public ImageFileInformation LargeImage { get; set; }

        public List<ImageFileInformation> SrcsetImages { get; set; }

        public ImageFileInformation DisplayImage { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.FileName} {x.Width}w"));
        }
    }
}
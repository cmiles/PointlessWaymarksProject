using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public class ImageDirectoryContentsInformation
    {
        public ImageFileInformation DisplayImage { get; set; }

        public ImageFileInformation LargeImage { get; set; }
        public ImageFileInformation SmallImage { get; set; }

        public List<ImageFileInformation> SrcsetImages { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.SiteUrl} {x.Width}w"));
        }
    }
}
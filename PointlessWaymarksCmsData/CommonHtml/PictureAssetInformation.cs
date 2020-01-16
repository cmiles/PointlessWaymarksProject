using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public class PictureAssetInformation
    {
        public object DbEntry { get; set; }
        public PictureFileInformation DisplayPicture { get; set; }
        public PictureFileInformation LargePicture { get; set; }
        public PictureFileInformation SmallPicture { get; set; }

        public List<PictureFileInformation> SrcsetImages { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.SiteUrl} {x.Width}w"));
        }
    }
}
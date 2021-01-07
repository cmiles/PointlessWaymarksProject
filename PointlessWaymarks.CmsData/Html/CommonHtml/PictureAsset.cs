using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public class PictureAsset
    {
        public object DbEntry { get; set; }
        public PictureFile DisplayPicture { get; set; }
        public PictureFile LargePicture { get; set; }
        public PictureFile SmallPicture { get; set; }

        public List<PictureFile> SrcsetImages { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.SiteUrl} {x.Width}w"));
        }
    }
}
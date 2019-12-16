using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLemmonWorkshopData
{
    public partial class SinglePhotoPage
    {
        public string ImageAlt { get; set; }

        public string SiteUrl { get; set; }

        public string Description { get; set; }
        public string SiteName { get; set; }
        public string PageUrl { get; set; }

        public string PageTitle { get; set; }

        public string DisplayImageUrl { get; set; }

        public int DisplayImageWidth { get; set; }

        public int DisplayImageHeight { get; set; }

        public List<SrcSetImage> SrcsetImages { get; set; }

        public string ToDisplayImage { get; set; }

        public DateTime? TakenOn { get; set; }

        public string TakenBy { get; set; }

        public string License { get; set; }

        public string Camera { get; set; }

        public string Lens { get; set; }

        public string Aperture { get; set; }

        public string ShutterSpeed { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ", SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.Url} {x.Width}w"));
        }
    }

    public class SrcSetImage
    {
        public int Width { get; set; }
        public string Url { get; set; }
    }
}
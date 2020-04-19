using System;
using System.Collections.Generic;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoGalleryHtml
{
    public partial class DailyPhotosPage
    {
        public string CreatedBy { get; set; }

        public List<PictureSiteInformation> ImageList { get; set; }

        public PictureSiteInformation MainImage { get; set; }

        public string PageUrl { get; set; }

        public DateTime PhotoPageDate { get; set; }

        public string PhotoTags { get; set; }

        public string SiteName { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
    }
}
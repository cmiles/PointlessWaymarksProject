using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopData.PhotoHtml;
using TheLemmonWorkshopWpfControls;

namespace TheLemmonWorkshopData.PostHtml
{
    public class SinglePostPageCode
    {
        public SinglePostPageCode(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);
        }


        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public PostContent DbEntry { get; set; }
    }
}
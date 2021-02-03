﻿using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml
{
    public partial class SingleGeoJsonPage
    {
        public SingleGeoJsonPage(GeoJsonContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.GeoJsonPageUrl(DbEntry);

            if (DbEntry.MainPicture != null) MainImage = new PictureSiteInformation(DbEntry.MainPicture.Value);
        }

        public GeoJsonContent DbEntry { get; }
        public DateTime? GenerationVersion { get; set; }
        public PictureSiteInformation? MainImage { get; }
        public string PageUrl { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }

        public async Task WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            await GeoJsonData.WriteJsonData(DbEntry);

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteGeoJsonHtmlFile(DbEntry);

            if (htmlFileInfo == null)
            {
                var toThrow =
                    new Exception("The GeoJson DbEntry did not have valid information to determine a file for the html");
                toThrow.Data.Add("GeoJson DbEntry", ObjectDumper.Dump(DbEntry));
                throw toThrow;
            }

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(htmlFileInfo.FullName, htmlString);
        }
    }
}
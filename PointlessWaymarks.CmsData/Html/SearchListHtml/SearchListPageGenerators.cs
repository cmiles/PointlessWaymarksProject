using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.TagListHtml;
using PointlessWaymarks.CmsData.Rss;

namespace PointlessWaymarks.CmsData.Html.SearchListHtml
{
    public static class SearchListPageGenerators
    {
        public const int MaxNumberOfRssEntries = 30;

        public static void WriteAllContentCommonSearchListHtml(DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                //!!Content Type List!!
                var db = Db.Context().Result;
                var fileContent = db.FileContents.Cast<object>().ToList();
                var geoJsonContent = db.GeoJsonContents.Cast<object>().ToList();
                var imageContent = db.ImageContents.Where(x => x.ShowInSearch).Cast<object>().ToList();
                var lineContent = db.LineContents.Cast<object>().ToList();
                var noteContent = db.NoteContents.Cast<object>().ToList();
                var photoContent = db.PhotoContents.Cast<object>().ToList();
                var pointContent = db.PointContents.Cast<object>().ToList();
                var postContent = db.PostContents.Cast<object>().ToList();

                return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                    .Concat(photoContent).Concat(pointContent).Concat(postContent)
                    .OrderBy(x => ((IContentCommon) x).Title).ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteAllContentListFile();

            WriteSearchListHtml(ContentList, fileInfo, "All Content",
                UserSettingsSingleton.CurrentSettings().AllContentRssUrl(), generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteAllContentRssFile(), "All Content",
                progress);
        }

        public static void WriteFileContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.FileContents.OrderBy(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteFileListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Files", UserSettingsSingleton.CurrentSettings().FileRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteFileRssFile(), "Files", progress);
        }


        public static void WriteImageContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.ImageContents.Where(x => x.ShowInSearch).OrderBy(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Images", UserSettingsSingleton.CurrentSettings().ImageRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteImageRssFile(), "Images", progress);
        }

        public static void WriteNoteContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.NoteContents.ToList().OrderByDescending(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteNoteListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Notes", UserSettingsSingleton.CurrentSettings().NoteRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteNoteRssFile(), "Notes", progress);
        }

        public static void WritePhotoContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.PhotoContents.OrderBy(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Photos", UserSettingsSingleton.CurrentSettings().PhotoRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePhotoRssFile(), "Photos", progress);
        }

        public static void WritePointContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.PointContents.OrderBy(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePointListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Points", UserSettingsSingleton.CurrentSettings().PointsRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePointRssFile(), "Points", progress);
        }

        public static void WritePostContentListHtml(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            static List<object> ContentList()
            {
                var db = Db.Context().Result;
                return db.PostContents.OrderBy(x => x.Title).Cast<object>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePostListFile();

            WriteSearchListHtml(ContentList, fileInfo, "Posts", UserSettingsSingleton.CurrentSettings().PostsRssUrl(),
                generationVersion, progress);
            RssBuilder.WriteContentCommonListRss(
                ContentList().Cast<IContentCommon>().OrderByDescending(x => x.CreatedOn).Take(MaxNumberOfRssEntries)
                    .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePostRssFile(), "Posts", progress);
        }

        public static void WriteSearchListHtml(Func<List<object>> dbFunc, FileInfo fileInfo, string titleAdd,
            string rssUrl, DateTime? generationVersion, IProgress<string>? progress = null)
        {
            progress?.Report($"Setting up Search List Page for {fileInfo.FullName}");

            var htmlModel = new SearchListPage(rssUrl)
            {
                ContentFunction = dbFunc, ListTitle = titleAdd, GenerationVersion = generationVersion
            };

            var htmlTransform = htmlModel.TransformText();

            progress?.Report($"Cleaning up Search List HTML and writing to {fileInfo.FullName}");

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(htmlTransform);

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                fileInfo.Refresh();
            }

            FileManagement.WriteAllTextToFileAndLog(fileInfo.FullName, htmlString);
        }

        public static void WriteTagList(DateTime generationVersion, IProgress<string>? progress = null)
        {
            progress?.Report("Tag Pages - Getting Tag Data For Search");
            var tags = Db.TagSlugsAndContentList(false, true, progress).Result;

            var allTags = new TagListPage {ContentFunction = () => tags, GenerationVersion = generationVersion};

            progress?.Report("Tag Pages - Writing All Tag Data");
            allTags.WriteLocalHtml();
        }

        public static void WriteTagListAndTagPages(DateTime? generationVersion, IProgress<string>? progress = null)
        {
            progress?.Report("Tag Pages - Getting Tag Data For Search");
            var tags = Db.TagSlugsAndContentList(false, true, progress).Result;

            var allTags = new TagListPage {ContentFunction = () => tags, GenerationVersion = generationVersion};

            progress?.Report("Tag Pages - Writing All Tag Data");
            allTags.WriteLocalHtml();

            progress?.Report("Tag Pages - Getting Tag Data For Page Generation");
            //Tags is reset - above for tag search we don't include tags from pages that are hidden from search - but to
            //ensure all tags have a page we generate pages from all tags (if an image excluded from search had a unique
            //tag we need a page for the links on that page, excluded from search does not mean 'unreachable'...)
            var pageTags = Db.TagSlugsAndContentList(true, false, progress).Result;

            var loopCount = 0;

            Parallel.ForEach(pageTags, loopTags =>
            {
                //loopCount++;

                //if (loopCount % 30 == 0)
                progress?.Report($"Generating Tag Page {loopTags.tag} - {loopCount} of {tags.Count}");

                WriteSearchListHtml(() => loopTags.contentObjects,
                    UserSettingsSingleton.CurrentSettings().LocalSiteTagListFileInfo(loopTags.tag),
                    $"Tag - {loopTags.tag}", string.Empty, generationVersion, progress);
            });
        }

        public static void WriteTagPage(string tag, List<dynamic> content, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            WriteSearchListHtml(() => content, UserSettingsSingleton.CurrentSettings().LocalSiteTagListFileInfo(tag),
                $"Tag - {tag}", string.Empty, generationVersion, progress);
        }
    }
}
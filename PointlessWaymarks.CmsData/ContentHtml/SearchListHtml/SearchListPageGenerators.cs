using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.TagListHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Rss;

namespace PointlessWaymarks.CmsData.ContentHtml.SearchListHtml;

public static class SearchListPageGenerators
{
    public const int MaxNumberOfRssEntries = 30;

    public static async Task WriteAllContentCommonSearchListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            //!!Content Type List!!
            var db = Db.Context().Result;
            var fileContent = db.FileContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var geoJsonContent = db.GeoJsonContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var imageContent = db.ImageContents.Where(x => !x.IsDraft && x.ShowInSearch).Cast<object>().ToList();
            var lineContent = db.LineContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var noteContent = db.NoteContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var photoContent = db.PhotoContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var pointContent = db.PointContents.Where(x => !x.IsDraft).Cast<object>().ToList();
            var postContent = db.PostContents.Where(x => !x.IsDraft).Cast<object>().ToList();

            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(photoContent).Concat(pointContent).Concat(postContent)
                .OrderBy(x => ((IContentCommon)x).Title).ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteAllContentListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "All Content",
                UserSettingsSingleton.CurrentSettings().AllContentRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteAllContentRssFile(), "All Content",
            progress);
    }

    public static async Task WriteFileContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.FileContents.Where(x => !x.IsDraft).OrderBy(x => x.Title).Cast<object>().ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteFileListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Files",
                UserSettingsSingleton.CurrentSettings().FileRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteFileRssFile(), "Files", progress);
    }


    public static async Task WriteImageContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.ImageContents.Where(x => !x.IsDraft && x.ShowInSearch).OrderBy(x => x.Title).Cast<object>()
                .ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Images",
                UserSettingsSingleton.CurrentSettings().ImageRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteImageRssFile(), "Images", progress);
    }

    public static async Task WriteNoteContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.NoteContents.Where(x => !x.IsDraft).ToList().OrderByDescending(x => x.Title).Cast<object>()
                .ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteNoteListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Notes",
                UserSettingsSingleton.CurrentSettings().NoteRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSiteNoteRssFile(), "Notes", progress);
    }

    public static async Task WritePhotoContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.PhotoContents.Where(x => !x.IsDraft).OrderBy(x => x.Title).Cast<object>().ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Photos",
                UserSettingsSingleton.CurrentSettings().PhotoRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePhotoRssFile(), "Photos", progress);
    }

    public static async Task WritePointContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.PointContents.Where(x => !x.IsDraft).OrderBy(x => x.Title).Cast<object>().ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePointListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Points",
                UserSettingsSingleton.CurrentSettings().PointsRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePointRssFile(), "Points", progress);
    }

    public static async Task WritePostContentListHtml(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        static List<object> ContentList()
        {
            var db = Db.Context().Result;
            return db.PostContents.Where(x => !x.IsDraft).OrderBy(x => x.Title).Cast<object>().ToList();
        }

        var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePostListFile();

        await WriteSearchListHtml(ContentList, fileInfo, "Posts",
                UserSettingsSingleton.CurrentSettings().PostsRssUrl(), generationVersion, progress)
            .ConfigureAwait(false);
        RssBuilder.WriteContentCommonListRss(
            ContentList().Cast<IContentCommon>().OrderByDescending(x => x.FeedOn).Take(MaxNumberOfRssEntries)
                .ToList(), UserSettingsSingleton.CurrentSettings().LocalSitePostRssFile(), "Posts", progress);
    }

    public static async Task WriteSearchListHtml(Func<List<object>> dbFunc, FileInfo fileInfo, string titleAdd,
        string rssUrl, DateTime? generationVersion, IProgress<string>? progress = null, bool addNoIndexTag = false)
    {
        progress?.Report($"Setting up Search List Page for {fileInfo.FullName}");

        var htmlModel = new SearchListPage(rssUrl, dbFunc, titleAdd, generationVersion)
        {
            AddNoIndexTag = addNoIndexTag
        };

        var htmlString = htmlModel.TransformText();

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
            fileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(fileInfo.FullName, htmlString).ConfigureAwait(false);
    }

    public static async Task WriteTagList(DateTime generationVersion, IProgress<string>? progress = null)
    {
        progress?.Report("Tag Pages - Getting Tag Data For Search");
        var tags = Db.TagSlugsAndContentList(false, true, progress).Result;

        var allTags = new TagListPage
        {
            ContentFunction = () => tags,
            GenerationVersion = generationVersion,
            LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute,
            DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute
        };

        progress?.Report("Tag Pages - Writing All Tag Data");
        await allTags.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task WriteTagListAndTagPages(DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report("Tag Pages - Getting Tag Data For Search");
        var tags = Db.TagSlugsAndContentList(false, true, progress).Result;

        var allTags = new TagListPage
        {
            ContentFunction = () => tags,
            GenerationVersion = generationVersion,
            LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute,
            DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute
        };

        progress?.Report("Tag Pages - Writing All Tag Data");
        await allTags.WriteLocalHtml().ConfigureAwait(false);

        progress?.Report("Tag Pages - Getting Tag Data For Page Generation");

        //Tags is reset - above for tag search we don't include tags from pages that are hidden from search - but to
        //ensure all tags have a page we generate pages from all tags (if an image excluded from search had a unique
        //tag we need a page for the links on that page, excluded from search does not mean 'unreachable'...)
        var pageTags = Db.TagSlugsAndContentList(true, false, progress).Result;
        var excludedTags = Db.TagExclusionSlugs().Result;

        await Parallel.ForEachAsync(pageTags, async (loopTags, _) =>
        {
            progress?.Report($"Generating Tag Page {loopTags.tag}");

            await WriteSearchListHtml(() => loopTags.contentObjects,
                UserSettingsSingleton.CurrentSettings().LocalSiteTagListFileInfo(loopTags.tag),
                $"Tag - {loopTags.tag}", string.Empty, generationVersion, progress,
                excludedTags.Contains(loopTags.tag)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public static async Task WriteTagPage(string tag, List<dynamic> content, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        await WriteSearchListHtml(() => content,
            UserSettingsSingleton.CurrentSettings().LocalSiteTagListFileInfo(tag), $"Tag - {tag}", string.Empty,
            generationVersion, progress).ConfigureAwait(false);
    }
}
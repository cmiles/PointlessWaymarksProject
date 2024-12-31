using HtmlTags;

namespace PointlessWaymarks.CmsData.BracketCodes;

public static class BracketCodeSpecialPages
{
    public const string FilesRssToken = "filesrss";
    public const string FilesSearchPageToken = "filesearchpage";
    public const string GeoJsonRssToken = "geojsonrss";
    public const string GeoJsonSearchPageToken = "geojsonsearchpage";
    public const string ImageRssToken = "imagesrss";
    public const string ImageSearchPageToken = "imagessearchpage";
    public const string IndexRssToken = "indexrss";
    public const string IndexToken = "index";
    public const string LatestContentPageToken = "latestcontentpage";
    public const string LinesRssToken = "linerss";
    public const string LinesSearchPageToken = "linesearchpage";
    public const string LinksRssToken = "linkrss";
    public const string LinksSearchPageToken = "linksearchpage";
    public const string MonthlyActivityToken = "monthlyactivity";
    public const string NotesRssToken = "noterss";
    public const string NotesSearchPageToken = "notesearchpage";
    public const string PhotoGalleryPageToken = "photogallerypage";
    public const string PhotosRssToken = "photorss";
    public const string PhotosSearchPageToken = "photosearchpage";
    public const string PointsRssToken = "pointrss";
    public const string PointsSearchPageToken = "pointsearchpage";
    public const string PostRssToken = "postrss";
    public const string PostsSearchPageToken = "postsearchpage";
    public const string SearchPageToken = "searchpage";
    public const string TagsPageToken = "tagspage";
    public const string TrailsRssToken = "trailrss";
    public const string TrailsSearchPageToken = "trailsearchpage";
    public const string VideosRssToken = "videorss";
    public const string VideosSearchPageToken = "videosearchpage";

    public static readonly string FilesRssBracketCode = $"{{{{{FilesRssToken}; text Files RSS Feed;}}}}";
    public static readonly string FilesSearchPageBracketCode = $"{{{{{FilesSearchPageToken}; text File Search;}}}}";
    public static readonly string GeoJsonRssBracketCode = $"{{{{{GeoJsonRssToken}; text GeoJson RSS Feed;}}}}";

    public static readonly string GeoJsonSearchPageBracketCode =
        $"{{{{{GeoJsonSearchPageToken}; text GeoJson Search;}}}}";

    public static readonly string ImageRssBracketCode = $"{{{{{ImageRssToken}; text Images RSS Feed;}}}}";
    public static readonly string ImagesSearchPageBracketCode = $"{{{{{ImageSearchPageToken}; text Image Search;}}}}";
    public static readonly string IndexBracketCode = $"{{{{{IndexToken}; text Main;}}}}";
    public static readonly string IndexRssBracketCode = $"{{{{{IndexRssToken}; text Main Page RSS Feed;}}}}";
    public static readonly string LatestContentPageBracketCode = $"{{{{{LatestContentPageToken}; text Latest;}}}}";
    public static readonly string LinesRssBracketCode = $"{{{{{LinesRssToken}; text Lines Rss Feed;}}}}";
    public static readonly string LinesSearchPageBracketCode = $"{{{{{LinesSearchPageToken}; text Line Search;}}}}";
    public static readonly string LinkRssBracketCode = $"{{{{{LinksRssToken}; text Links RSS Feed;}}}}";
    public static readonly string LinksSearchPageBracketCode = $"{{{{{LinksSearchPageToken}; text Link Search;}}}}";

    public static readonly string MonthlyActivityBracketCode =
        $"{{{{{MonthlyActivityToken}; text Monthly Activities;}}}}";

    public static readonly string NoteRssBracketCode = $"{{{{{NotesRssToken}; text Notes RSS Feed;}}}}";
    public static readonly string NotesSearchPageBracketCode = $"{{{{{NotesSearchPageToken}; text Note Search;}}}}";
    public static readonly string PhotoGalleryPageBracketCode = $"{{{{{PhotoGalleryPageToken}; text Photos;}}}}";
    public static readonly string PhotoRssBracketCode = $"{{{{{PhotosRssToken}; text Photos RSS Feed;}}}}";
    public static readonly string PhotosSearchPageBracketCode = $"{{{{{PhotosSearchPageToken}; text Photo Search;}}}}";
    public static readonly string PointsRssBracketCode = $"{{{{{PointsRssToken}; text Points RSS Feed;}}}}";
    public static readonly string PointsSearchPageBracketCode = $"{{{{{PointsSearchPageToken}; text Point Search;}}}}";
    public static readonly string PostRssBracketCode = $"{{{{{PostRssToken}; text Posts RSS Feed;}}}}";
    public static readonly string PostSearchPageBracketCode = $"{{{{{PostsSearchPageToken}; text Post Search;}}}}";
    public static readonly string SearchPageBracketCode = $"{{{{{SearchPageToken}; text Search;}}}}";
    public static readonly string TagsPageBracketCode = $"{{{{{TagsPageToken}; text Tags;}}}}";
    public static readonly string TrailsRssBracketCode = $"{{{{{TrailsRssToken}; text Trails RSS Feed;}}}}";
    public static readonly string TrailsSearchPageBracketCode = $"{{{{{TrailsSearchPageToken}; text Trail Search;}}}}";
    public static readonly string VideoRssBracketCode = $"{{{{{VideosRssToken}; text Video RSS Feed;}}}}";
    public static readonly string VideoSearchPageBracketCode = $"{{{{{VideosSearchPageToken}; text Video Search;}}}}";

    public static string Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for Special Page Codes");

        var specialPageLookup = new List<(string bracketCode, string defaultDisplayString, string url)>
        {
            (IndexToken, "Main", $"{UserSettingsSingleton.CurrentSettings().IndexPageUrl()}"),
            (IndexRssToken, "Main Page RSS Feed", $"{UserSettingsSingleton.CurrentSettings().RssIndexFeedUrl()}"),
            (PhotoGalleryPageToken, "Photos", $"{UserSettingsSingleton.CurrentSettings().CameraRollGalleryUrl()}"),
            (LatestContentPageToken, "Latest", $"{UserSettingsSingleton.CurrentSettings().LatestContentGalleryUrl()}"),
            (SearchPageToken, "Search", $"{UserSettingsSingleton.CurrentSettings().AllContentListUrl()}"),
            (TagsPageToken, "Tags", $"{UserSettingsSingleton.CurrentSettings().AllTagsListUrl()}"),
            (MonthlyActivityToken, "Monthly Activities",
                $"{UserSettingsSingleton.CurrentSettings().LineMonthlyActivitySummaryUrl()}"),

            (FilesRssToken, "Files RSS Feed", $"{UserSettingsSingleton.CurrentSettings().FilesRssUrl()}"),
            (GeoJsonRssToken, "GeoJson RSS Feed", $"{UserSettingsSingleton.CurrentSettings().GeoJsonRssUrl()}"),
            (ImageRssToken, "Images RSS Feed", $"{UserSettingsSingleton.CurrentSettings().ImagesRssUrl()}"),
            (LinesRssToken, "Lines RSS Feed", $"{UserSettingsSingleton.CurrentSettings().LinesRssUrl()}"),
            (LinksRssToken, "Links RSS Feed", $"{UserSettingsSingleton.CurrentSettings().LinksRssUrl()}"),
            (NotesRssToken, "Notes RSS Feed", $"{UserSettingsSingleton.CurrentSettings().NotesRssUrl()}"),
            (PhotosRssToken, "Photos RSS Feed", $"{UserSettingsSingleton.CurrentSettings().PhotosRssUrl()}"),
            (PointsRssToken, "Points RSS Feed", $"{UserSettingsSingleton.CurrentSettings().PointsRssUrl()}"),
            (PostRssToken, "Posts RSS Feed", $"{UserSettingsSingleton.CurrentSettings().PostsRssUrl()}"),
            (TrailsRssToken, "Trails RSS Feed", $"{UserSettingsSingleton.CurrentSettings().TrailsRssUrl()}"),
            (VideosRssToken, "Videos RSS Feed", $"{UserSettingsSingleton.CurrentSettings().VideoRssUrl()}"),
            
            (FilesSearchPageToken, "File Search", $"{UserSettingsSingleton.CurrentSettings().FilesListUrl()}"),
            (GeoJsonSearchPageToken, "GeoJson Search", $"{UserSettingsSingleton.CurrentSettings().GeoJsonListUrl()}"),
            (ImageSearchPageToken, "Image Search", $"{UserSettingsSingleton.CurrentSettings().ImagesListUrl()}"),
            (LinesSearchPageToken, "Line Search", $"{UserSettingsSingleton.CurrentSettings().LinesListUrl()}"),
            (LinksSearchPageToken, "Link Search", $"{UserSettingsSingleton.CurrentSettings().LinksListUrl()}"),
            (NotesSearchPageToken, "Note Search", $"{UserSettingsSingleton.CurrentSettings().NotesListUrl()}"),
            (PhotosSearchPageToken, "Photo Search", $"{UserSettingsSingleton.CurrentSettings().PhotosListUrl()}"),
            (PointsSearchPageToken, "Point Search", $"{UserSettingsSingleton.CurrentSettings().PointsListUrl()}"),
            (PostsSearchPageToken, "Post Search", $"{UserSettingsSingleton.CurrentSettings().PostsListUrl()}"),
            (TrailsSearchPageToken, "Trail Search", $"{UserSettingsSingleton.CurrentSettings().TrailsListUrl()}"),
            (VideosSearchPageToken, "Video Search", $"{UserSettingsSingleton.CurrentSettings().VideosListUrl()}"),
        };

        foreach (var loopLookups in specialPageLookup)
        {
            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(toProcess, loopLookups.bracketCode);

            foreach (var loopMatch in matches)
            {
                progress?.Report($"Adding Special Page {loopLookups.bracketCode} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? loopLookups.defaultDisplayString
                            : loopMatch.displayText.Trim(), loopLookups.url, "special-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }
        }

        return toProcess;
    }
}
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.ContentHtml.ErrorHtml;

public partial class ErrorPage
{
    private const int NumberOfContentItemsToDisplay = 4;

    public ErrorPage()
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl;
        SiteName = settings.SiteName;
        SiteKeywords = settings.SiteKeywords;
        SiteSummary = settings.SiteSummary;
        SiteAuthors = settings.SiteAuthors;
        PageUrl = settings.IndexPageUrl();
        LangAttribute = settings.SiteLangAttribute;
        DirAttribute = settings.SiteDirectionAttribute;
    }

    public string DirAttribute { get; set; }

    public DateTime? GenerationVersion { get; set; }

    public string LangAttribute { get; set; }
    public string PageUrl { get; }
    public string SiteAuthors { get; }
    public string SiteKeywords { get; }
    public string SiteName { get; }
    public string SiteSummary { get; }
    public string SiteUrl { get; }

    public async Task WriteLocalHtml()
    {
        var parser = new HtmlParser();
        var htmlDoc = parser.ParseDocument(TransformText());

        var stringWriter = new StringWriter();
        htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

        var htmlString = stringWriter.ToString();

        var htmlFileInfo =
            new FileInfo($@"{UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName}\error.html");

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}
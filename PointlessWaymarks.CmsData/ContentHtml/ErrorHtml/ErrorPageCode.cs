using System.Text.RegularExpressions;

namespace PointlessWaymarks.CmsData.ContentHtml.ErrorHtml;

public partial class ErrorPage
{
    public ErrorPage()
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
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

    public async Task WriteLocalHtml(bool writeOnlyIfChanged = false)
    {
        var htmlString = TransformText();

        var htmlFileInfo =
            new FileInfo(
                $@"{UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName}\error.html");

        if (htmlFileInfo.Exists)
        {
            if (writeOnlyIfChanged)
            {
                var currentFileString = await htmlFileInfo.OpenText().ReadToEndAsync();
                var pattern = "data-generationversion=\"[^\"\"]*\"";

                var currentTextString = Regex.Replace(currentFileString, pattern, string.Empty);
                var newTextString = Regex.Replace(htmlString, pattern, string.Empty);

                if (currentTextString.Equals(newTextString)) return;
            }

            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}
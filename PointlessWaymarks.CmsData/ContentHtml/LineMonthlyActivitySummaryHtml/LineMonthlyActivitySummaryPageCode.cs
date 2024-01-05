using System.Text.Json;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsData.ContentHtml.LineMonthlyActivitySummaryHtml;

public partial class LineMonthlyActivitySummaryPage
{
    public LineMonthlyActivitySummaryPage(DateTime? generationVersion)
    {
        GenerationVersion = generationVersion;
        LangAttribute = UserSettingsSingleton.CurrentSettings().SiteLangAttribute;
        DirAttribute = UserSettingsSingleton.CurrentSettings().SiteDirectionAttribute;
    }

    public string DirAttribute { get; set; }
    public DateTime? GenerationVersion { get; set; }
    public string LangAttribute { get; set; }
    public string SerializedRows { get; set; }

    public async Task WriteLocalHtml()
    {
        var lineContent = (await Db.Context()).LineContents.LineContentFilteredForActivities().ToList();

        var grouped = lineContent.GroupBy(x =>
                new { x.RecordingStartedOn!.Value.Year, x.RecordingStartedOn.Value.Month })
            .OrderByDescending(x => x.Key.Year).ThenByDescending(x => x.Key.Month);

        var reportRows = grouped.Select(x => new
        {
            Year = x.Key.Year,
            Month = x.Key.Month,
            Activities = x.Count(),
            Miles = Math.Floor(x.Sum(y => y.LineDistance)),
            Hours = Math.Floor(new TimeSpan(0, (int)x
                .Select(y => y.RecordingEndedOn.Value - y.RecordingStartedOn.Value)
                .Sum(y => y.TotalMinutes), 0).TotalHours),
            MinElevation = Math.Floor(x.Min(y => y.MinimumElevation)),
            MaxElevation = Math.Floor(x.Max(y => y.MaximumElevation)),
            Climb = Math.Floor(x.Sum(y => y.ClimbElevation)),
            Descent = Math.Floor(x.Sum(y => y.DescentElevation))
        }).ToList();

        SerializedRows = JsonSerializer.Serialize(reportRows);

        var settings = UserSettingsSingleton.CurrentSettings();

        var htmlString = TransformText();

        var htmlFileInfo = settings.LocalSiteLineMonthlyActivityHtmlFile();

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }
}
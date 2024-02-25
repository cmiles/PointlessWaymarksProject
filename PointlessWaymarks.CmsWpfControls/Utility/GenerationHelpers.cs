using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsWpfControls.Diagnostics;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class GenerationHelpers
{
    public static async Task GenerateChangedHtml(IProgress<string> progress)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var generationResults = await SiteGeneration.ChangedSiteContent(progress);

        if (generationResults.All(x => !x.HasError)) return;

        await Reports.InvalidBracketCodeContentIdsHtmlReport(generationResults);
    }
}
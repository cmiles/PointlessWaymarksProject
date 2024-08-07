using System.Text;
using System.Web;
using HtmlTableHelper;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.Diagnostics;

public static class Reports
{
    public static async Task GenerationListHtmlReport(List<GenerationReturn> generationReturns, string title,
        string intro)
    {
        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine($"<p>{HttpUtility.HtmlEncode(intro)}</p>");
        bodyBuilder.AppendLine(generationReturns.ToHtmlTable(new { @class = "pure-table pure-table-striped" }));

        var reportWindow = await WebViewWindow.CreateInstance();
        await reportWindow.PositionWindowAndShowOnUiThread();

        await reportWindow.SetupDocumentWithPureCss(bodyBuilder.ToString(), title);
    }

    public static async Task InvalidBracketCodeContentIdsHtmlReport(List<GenerationReturn> generationReturns)
    {
        await GenerationListHtmlReport(generationReturns.Where(x => x.HasError).ToList(),
                "Bracket Codes with Invalid Content Ids",
                "The content listed below contains one or more bracket codes where the ContentId is not valid. This can happen when deleting content and can be particularly unexpected when deleting and re-creating content...")
            .ConfigureAwait(false);
    }
}
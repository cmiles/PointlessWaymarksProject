using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.TrailHtml;

public static class Email
{
    public static async Task<string> ToHtmlEmail(TrailContent? content, IProgress<string>? progress = null)
    {
        if (content == null) return string.Empty;

        var preprocessResults = await BracketCodeCommon.ProcessCodesForEmail(content.BodyContent, progress).ConfigureAwait(false);
        var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

        var tags = Tags.TagListTextLinkList(content);
        tags.Style("text-align", "center");

        var createdUpdated = $"<p style=\"text-align: center;\">{Tags.CreatedByAndUpdatedOnString(content)}</p>";

        var innerContent = HtmlEmail.ChildrenIntoTableCells(
            $"{await HtmlEmail.EmailSimpleTitle(content).ConfigureAwait(false)}{bodyHtmlString}{tags}{createdUpdated}{HtmlEmail.EmailSimpleFooter()}");

        var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

        return emailHtml;
    }
}
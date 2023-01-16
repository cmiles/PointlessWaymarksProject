using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.VideoHtml;

public static class Email
{
    public static async Task<string> ToHtmlEmail(VideoContent content, IProgress<string>? progress = null)
    {
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
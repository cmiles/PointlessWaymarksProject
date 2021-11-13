using System.Text;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.ImageHtml;

public static class Email
{
    public static async Task<string> ToHtmlEmail(ImageContent content, IProgress<string>? progress = null)
    {
        var mdBuilder = new StringBuilder();

        mdBuilder.AppendLine(BracketCodeImages.Create(content));

        if (!string.IsNullOrWhiteSpace(content.BodyContent))
        {
            mdBuilder.AppendLine();

            mdBuilder.AppendLine(content.BodyContent);
        }

        var tags = Tags.TagListTextLinkList(content);
        tags.Style("text-align", "center");
        mdBuilder.AppendLine(tags.ToString());

        mdBuilder.AppendLine();

        mdBuilder.AppendLine($"<p style=\"text-align: center;\">{Tags.CreatedByAndUpdatedOnString(content)}</p>");

        var preprocessResults = await BracketCodeCommon.ProcessCodesForEmail(mdBuilder.ToString(), progress).ConfigureAwait(false);
        var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

        var innerContent = HtmlEmail.ChildrenIntoTableCells(
            $"{await HtmlEmail.EmailSimpleTitle(content).ConfigureAwait(false)}{bodyHtmlString}{HtmlEmail.EmailSimpleFooter()}");

        var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

        return emailHtml;
    }
}
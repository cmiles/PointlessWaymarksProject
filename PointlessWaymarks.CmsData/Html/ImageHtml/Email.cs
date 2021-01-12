using System;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.ImageHtml
{
    public static class Email
    {
        public static async Task<string> ToHtmlEmail(ImageContent content, IProgress<string>? progress = null)
        {
            if (content == null) return string.Empty;

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

            var preprocessResults = BracketCodeCommon.ProcessCodesForEmail(mdBuilder.ToString(), progress);
            var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

            var innerContent = HtmlEmail.ChildrenIntoTableCells(
                $"{await HtmlEmail.EmailSimpleTitle(content)}{bodyHtmlString}{HtmlEmail.EmailSimpleFooter()}");

            var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

            return emailHtml;
        }
    }
}
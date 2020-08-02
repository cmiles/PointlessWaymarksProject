using System;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.NoteHtml
{
    public static class Email
    {
        public static async Task<string> ToHtmlEmail(NoteContent content, IProgress<string> progress)
        {
            if (content == null) return string.Empty;

            var preprocessResults = BracketCodeCommon.ProcessCodesForEmail(content.BodyContent, progress);
            var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

            var tags = Tags.TagListTextLinkList(content);
            tags.Style("text-align", "center");

            var createdUpdated = $"<p style=\"text-align: center;\">{Tags.CreatedByAndUpdatedOnString(content)}</p>";

            var innerContent =
                HtmlEmail.ChildrenIntoTableCells(
                    $"{await HtmlEmail.EmailSimpleTitle(content)}{bodyHtmlString}{tags}{createdUpdated}{HtmlEmail.EmailSimpleFooter()}");

            var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

            return emailHtml;
        }
    }
}

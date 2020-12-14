using System;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.FileHtml
{
    public static class Email
    {
        public static async Task<string> ToHtmlEmail(FileContent content, IProgress<string> progress)
        {
            if (content == null) return string.Empty;

            var preprocessResults = BracketCodeCommon.ProcessCodesForEmail(content.BodyContent, progress);
            var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

            var tags = Tags.TagListTextLinkList(content);
            tags.Style("text-align", "center");

            var createdUpdated = $"<p style=\"text-align: center;\">{Tags.CreatedByAndUpdatedOnString(content)}</p>";

            var possibleDownloadLink = FileParts.DownloadLinkTag(content);
            possibleDownloadLink.Style("text-align", "center");

            var innerContent = HtmlEmail.ChildrenIntoTableCells(
                $"{await HtmlEmail.EmailSimpleTitle(content)}{bodyHtmlString}{possibleDownloadLink}{tags}{createdUpdated}{HtmlEmail.EmailSimpleFooter()}");

            var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

            return emailHtml;
        }
    }
}
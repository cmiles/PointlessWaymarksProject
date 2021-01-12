using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.PhotoHtml
{
    public static class Email
    {
        public static async Task<string> ToHtmlEmail(PhotoContent? content, IProgress<string>? progress = null)
        {
            if (content == null) return string.Empty;

            var mdBuilder = new StringBuilder();

            mdBuilder.AppendLine(BracketCodePhotos.Create(content));

            var detailsList = new List<string?>
            {
                content.Aperture,
                content.ShutterSpeed,
                content.Iso?.ToString("F0"),
                content.Lens,
                content.FocalLength,
                content.CameraMake,
                content.CameraModel,
                content.License
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            mdBuilder.AppendLine($"<p style=\"text-align: center;\">Details: {string.Join(", ", detailsList)}</p>");

            var tags = Tags.TagListTextLinkList(content);
            tags.Style("text-align", "center");
            mdBuilder.AppendLine(tags.ToString());

            mdBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(content.BodyContent)) mdBuilder.AppendLine(content.BodyContent);

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
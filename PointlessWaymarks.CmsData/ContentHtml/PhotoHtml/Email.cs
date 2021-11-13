using System.Text;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.PhotoHtml
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

            var preprocessResults = await BracketCodeCommon.ProcessCodesForEmail(mdBuilder.ToString(), progress).ConfigureAwait(false);
            var bodyHtmlString = ContentProcessing.ProcessContent(preprocessResults, content.BodyContentFormat);

            var innerContent = HtmlEmail.ChildrenIntoTableCells(
                $"{await HtmlEmail.EmailSimpleTitle(content).ConfigureAwait(false)}{bodyHtmlString}{HtmlEmail.EmailSimpleFooter()}");

            var emailHtml = HtmlEmail.WrapInNestedCenteringTable(innerContent);

            return emailHtml;
        }
    }
}
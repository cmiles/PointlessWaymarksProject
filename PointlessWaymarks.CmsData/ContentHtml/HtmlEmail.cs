using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using HtmlTags;

namespace PointlessWaymarks.CmsData.ContentHtml
{
    public static class HtmlEmail
    {
        public static string ChildrenIntoTableCells(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var document = parser.ParseDocument(html);

            var tableBuilder = new StringBuilder();

            var childNodes = document.QuerySelector("body").ChildNodes.Where(x => x.NodeType != NodeType.Text).ToList();

            foreach (var topNodes in childNodes) tableBuilder.AppendLine($"<tr><td>{topNodes.ToHtml()}</td></tr>");

            return tableBuilder.ToString();
        }

        public static HtmlTag EmailSimpleFooter()
        {
            var footer = new HtmlTag("h4");
            footer.Style("text-align", "center");
            var siteLink = new LinkTag(UserSettingsSingleton.CurrentSettings().SiteUrl,
                @$"https://{UserSettingsSingleton.CurrentSettings().SiteUrl}");
            footer.Children.Add(siteLink);

            return footer;
        }

        public static async Task<HtmlTag> EmailSimpleTitle(dynamic content)
        {
            Guid contentId = content.ContentId;
            string title = content.Title;

            var header = new HtmlTag("h3");
            header.Style("text-align", "center");
            var postAddress = $"https:{await UserSettingsSingleton.CurrentSettings().ContentUrl(contentId).ConfigureAwait(false)}";
            var postLink = new LinkTag($"{UserSettingsSingleton.CurrentSettings().SiteName} - {title}", postAddress);
            header.Children.Add(postLink);

            return header;
        }

        public static string WrapInNestedCenteringTable(string htmlString)
        {
            // ReSharper disable StringLiteralTypo
            var emailCenterTable = new TableTag();
            emailCenterTable.Attr("width", "100%");
            emailCenterTable.Attr("border", "0");
            emailCenterTable.Attr("cellspacing", "0");
            emailCenterTable.Attr("cellpadding", "0");
            var emailCenterRow = emailCenterTable.AddBodyRow();

            var emailCenterLeftCell = emailCenterRow.Cell();
            emailCenterLeftCell.Attr("max-width", "1%");
            emailCenterLeftCell.Attr("align", "center");
            emailCenterLeftCell.Attr("valign", "top");
            emailCenterLeftCell.Text("&nbsp;").Encoded(false);

            var emailCenterContentCell = emailCenterRow.Cell();
            emailCenterContentCell.Attr("width", "100%");
            emailCenterContentCell.Attr("align", "center");
            emailCenterContentCell.Attr("valign", "top");

            var emailCenterRightCell = emailCenterRow.Cell();
            emailCenterRightCell.Attr("max-width", "1%");
            emailCenterRightCell.Attr("align", "center");
            emailCenterRightCell.Attr("valign", "top");
            emailCenterRightCell.Text("&nbsp;").Encoded(false);
            // ReSharper restore StringLiteralTypo

            var outerTable = new TableTag();
            emailCenterContentCell.Children.Add(outerTable);
            outerTable.Style("width", "100%");
            outerTable.Style("max-width", "900px");

            outerTable.TBody.Text(htmlString).Encoded(false);

            return emailCenterTable.ToString();
        }
    }
}
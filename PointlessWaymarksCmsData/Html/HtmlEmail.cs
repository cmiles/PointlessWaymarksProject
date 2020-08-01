using System.Linq;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using HtmlTags;

namespace PointlessWaymarksCmsData.Html
{
    public static class HtmlEmail
    {
        public static string FromHtml(string bodyHtmlString)
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var document = parser.ParseDocument(bodyHtmlString);

            var tableBuilder = new StringBuilder();

            var childNodes = document.QuerySelector("body").ChildNodes.Where(x => x.NodeType != NodeType.Text).ToList();

            foreach (var topNodes in childNodes) tableBuilder.AppendLine($"<tr><td>{topNodes.ToHtml()}</td></tr>");

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

            outerTable.TBody.Text(tableBuilder.ToString()).Encoded(false);

            return emailCenterTable.ToString();
        }
    }
}
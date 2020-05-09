using System.Text.Encodings.Web;

namespace PointlessWaymarksCmsWpfControls.WpfHtml
{
    public static class WpfHtmlDocument
    {
        public static string ToHtmlDocument(this string body, string title, string styleBlock)
        {
            var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta charset=utf-8>
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{styleBlock}</style>
</head>
<body>
    {body}
</body>";

            return htmlDoc;
        }

        public static string ToHtmlDocumentWithPureCss(this string body, string title, string styleBlock)
        {
            var pureInline = new PureInlineCss();
            var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta charset=utf-8>
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{pureInline.TransformText()} {styleBlock}</style>
</head>
<body>
    {body}
</body>";

            return htmlDoc;
        }
    }
}
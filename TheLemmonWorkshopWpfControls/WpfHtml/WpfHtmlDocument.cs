using System.Text.Encodings.Web;

namespace TheLemmonWorkshopWpfControls.WpfHtml
{
    public static class WpfHtmlDocument
    {
        public static string ToHtmlDocument(this string body, string title, string styleBlock)
        {
            return $@"
<!doctype html>
<html lang=en>
<head>
   <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
   <meta charset=utf-8>
   <style>{styleBlock}</style>
   <title>{HtmlEncoder.Default.Encode(title)}</title>
</head>
<body>
{body}
</body>";
        }
    }
}
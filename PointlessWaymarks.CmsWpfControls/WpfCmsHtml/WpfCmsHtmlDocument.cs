using System.Text.Encodings.Web;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class WpfCmsHtmlDocument
{
    public static async Task<FileBuilder> CmsLeafletSpatialScriptHtmlAndJs(string body,
        string title, string styleBlock)
    {
        var spatialScript = await FileManagement.SpatialScriptsAsString();

        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                              <meta name="viewport" content="width=device-width, initial-scale=1.0">
                              <meta charset="utf-8">
                              <title>{HtmlEncoder.Default.Encode(title)}</title>
                              <style>{styleBlock}</style>
                              <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                              <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                              <script src="pointless-waymarks-spatial-common.js"></script>
                          </head>
                          <body>
                              {body}
                          </body>
                          </html>
                          """;

        var toReturn = new FileBuilder();
        toReturn.Create.Add(new FileBuilderCreate("pointless-waymarks-spatial-common.js", spatialScript));
        toReturn.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return toReturn;
    }
}
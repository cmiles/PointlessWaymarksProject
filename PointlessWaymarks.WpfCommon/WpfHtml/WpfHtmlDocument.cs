using System.Text.Encodings.Web;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtmlResources;

// ReSharper disable StringLiteralTypo

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public static class WpfHtmlDocument
{
    public static FileBuilder CmsLeafletMapHtmlAndJs(string title, string styleBlock = "", string javascript = "")
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                            <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                            <meta charset="utf-8">
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <title>{HtmlEncoder.Default.Encode(title)}</title>
                            <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                            <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                            <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                            <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                            <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                              {(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}
                              {(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}
                          </head>
                          <body initialDocumentLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 96vh;"></div>
                          </body>
                          </html>
                          """;

        var initialWebFilesMessage = new FileBuilder();

        if (!string.IsNullOrWhiteSpace(styleBlock))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customStyle.css", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("leafletBingLayer.js",
            WpfHtmlResourcesHelper.LeafletBingLayerJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("localMapCommon.js",
            WpfHtmlResourcesHelper.LocalMapCommonJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    public static FileBuilder CmsLeafletMapAndChartHtmlAndJs(string title, double initialLatitude,
        double initialLongitude, string styleBlock = "", string javascript = "")
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                            <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                            <meta charset="utf-8">
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <title>{HtmlEncoder.Default.Encode(title)}</title>
                            <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                            <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                            <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                            <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                            <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                              {(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}
                              {(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}
                          </head>
                          <body onload="initialDocumentLoad();">
                            <div style="display: flex; justify-content: center; align-items:center; flex-direction: column">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 50vh;"></div>
                                <div id="mainElevationChartContainer" style="height: 10vh;" class="line-elevation-chart-container">
                                    <canvas id="mainElevationChart" class="line-elevation-chart"></canvas>
                                </div>
                            </div>
                          </body>
                          </html>
                          """;

        var initialWebFilesMessage = new FileBuilder();

        if (!string.IsNullOrWhiteSpace(styleBlock))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customStyle.css", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("leafletBingLayer.js",
            WpfHtmlResourcesHelper.LeafletBingLayerJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("localMapCommon.js",
            WpfHtmlResourcesHelper.LocalMapCommonJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    public static void SetupCmsLeafletMapHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, string calTopoApiKey = "", string bingApiKey = "", string cssStyleBlock = "", string javascript = "")
    {
        var initialWebFilesMessage = CmsLeafletMapHtmlAndJs(title, cssStyleBlock, javascript);

        messenger.ToWebView.Enqueue(initialWebFilesMessage);

        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        messenger.ToWebView.Enqueue(ExecuteJavaScript.CreateRequest(
            $"initialMapLoad({initialLatitude}, {initialLongitude}, '{calTopoApiKey}', '{bingApiKey}'"));
    }

    public static async Task SetupDocumentWithMinimalCss(this IWebViewMessenger messenger, string body,
        string title, string styleBlock = "", string javascript = "")
    {
        var minimalCss = await HtmlTools.MinimalCssAsString();

        var htmlDoc = $$"""

                        <!doctype html>
                        <html lang=en>
                        <head>
                            <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <meta charset="utf-8">
                            <title>{{HtmlEncoder.Default.Encode(title)}}</title>
                            <link rel="stylesheet" href="https://[[VirtualDomain]]/minimal.css" />
                            {{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}
                            {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}
                        </head>
                        <body>
                            {{body}}
                            <script>
                                window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
                            </script>
                        </body>
                        </html>
                        """;

        var initialWebFilesMessage = new FileBuilder();

        if (!string.IsNullOrWhiteSpace(minimalCss))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("minimal.css", minimalCss));
        if (!string.IsNullOrWhiteSpace(styleBlock))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customStyle.js", htmlDoc));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customScript.js", htmlDoc));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlDoc, true));

        messenger.ToWebView.Enqueue(initialWebFilesMessage);
        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));
    }

    public static async Task SetupDocumentWithPureCss(this IWebViewMessenger messenger, string body,
        string title, string styleBlock = "", string javascript = "")
    {
        var pureCss = await HtmlTools.PureCssAsString();

        var htmlDoc = $$"""

                        <!doctype html>
                        <html lang=en>
                        <head>
                            <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <meta charset="utf-8">
                            <title>{{HtmlEncoder.Default.Encode(title)}}</title>
                            <link rel="stylesheet" href="https://[[VirtualDomain]]/pure.css" />
                            {{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}
                            {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}
                        </head>
                        <body>
                            {{body}}
                            <script>
                                window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
                            </script>
                        </body>
                        </html>
                        """;

        var initialWebFilesMessage = new FileBuilder();

        if (!string.IsNullOrWhiteSpace(pureCss))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("pure.css", pureCss));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customStyle.js", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlDoc, true));

        messenger.ToWebView.Enqueue(initialWebFilesMessage);
        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));
    }
}
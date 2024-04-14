using System.Text.Encodings.Web;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtmlResources;

// ReSharper disable StringLiteralTypo

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public static class WpfHtmlDocument
{
    public static FileBuilder CmsLeafletMapAndChartHtmlAndJs(string title, string styleBlock = "",
        string javascript = "", string serializedMapIcons = "")
    {
        var htmlString = $$"""
                           <!doctype html>
                           <html lang=en>
                           <head>
                             <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                             <meta charset="utf-8">
                             <meta name="viewport" content="width=device-width, initial-scale=1.0">
                             <title>{{HtmlEncoder.Default.Encode(title)}}</title>
                             <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                             <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                             <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
                             <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                             <link rel="stylesheet" href="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.css" />
                             <script src="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.js"></script>
                             <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                               {{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}
                               {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}
                           </head>
                           <body onload="initialDocumentLoad();">
                             <div style="display: grid; grid-template-rows: auto 150px; height: 95vh; width: 100%;">
                                <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                   style="grid-row-start: 1; position: relative;"></div>
                                 <div id="mainElevationChartContainer" style=" grid-row-start: 2;" class="line-elevation-chart-container">
                                     <canvas id="mainElevationChart" class="line-elevation-chart" style="max-width:100%;"></canvas>
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
        initialWebFilesMessage.Create.AddRange(WpfHtmlResourcesHelper.AwesomeMapSvgMarkers());
        if (!string.IsNullOrWhiteSpace(serializedMapIcons))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("pwMapSvgIcons.json", serializedMapIcons));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    public static FileBuilder CmsLeafletMapHtmlAndJs(string title, string styleBlock = "", string javascript = "", string serializedMapIcons = "")
    {
        var htmlString = $$"""
                           <!doctype html>
                           <html lang=en>
                           <head>
                             <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                             <meta charset="utf-8">
                             <meta name="viewport" content="width=device-width, initial-scale=1.0">
                             <title>{{HtmlEncoder.Default.Encode(title)}}</title>
                             <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                             <link rel="stylesheet" href="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.css">
                             <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                             <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
                             <script src="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.js"></script>
                             <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                             <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                               {{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}
                               {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}
                           </head>
                           <body onload="initialDocumentLoad();">
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
        initialWebFilesMessage.Create.AddRange(WpfHtmlResourcesHelper.AwesomeMapSvgMarkers());
        if (!string.IsNullOrWhiteSpace(serializedMapIcons))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("pwMapSvgIcons.json", serializedMapIcons));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    public static void SetupCmsLeafletMapHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, bool autoCloseMarkers, string serializedMapIcons = "", string calTopoApiKey = "",
        string bingApiKey = "", string cssStyleBlock = "", string javascript = "")
    {
        var initialWebFilesMessage = CmsLeafletMapHtmlAndJs(title, cssStyleBlock, javascript, serializedMapIcons);

        messenger.ToWebView.Enqueue(initialWebFilesMessage);

        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        messenger.ToWebView.Enqueue(ExecuteJavaScript.CreateRequest(
            $"initialMapLoad({initialLatitude}, {initialLongitude}, '{calTopoApiKey}', '{bingApiKey}', true, {autoCloseMarkers.ToString().ToLower()})",
            true));
    }

    public static void SetupCmsLeafletMapWithLineElevationChartHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, string serializedMapIcons = "", string calTopoApiKey = "", string bingApiKey = "",
        string cssStyleBlock = "", string javascript = "")
    {
        var initialWebFilesMessage = CmsLeafletMapAndChartHtmlAndJs(title, cssStyleBlock, javascript, serializedMapIcons);

        messenger.ToWebView.Enqueue(initialWebFilesMessage);

        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        messenger.ToWebView.Enqueue(ExecuteJavaScript.CreateRequest(
            $"initialMapLoad({initialLatitude}, {initialLongitude}, '{calTopoApiKey}', '{bingApiKey}', true, true)",
            true));
    }

    public static void SetupCmsLeafletPointChooserMapHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, string serializedMapIcons = "", string calTopoApiKey = "", string bingApiKey = "",
        string cssStyleBlock = "", string javascript = "")
    {
        var initialWebFilesMessage = CmsLeafletMapHtmlAndJs(title, cssStyleBlock, javascript, serializedMapIcons);

        messenger.ToWebView.Enqueue(initialWebFilesMessage);

        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        messenger.ToWebView.Enqueue(ExecuteJavaScript.CreateRequest(
            $"initialMapLoadWithUserPointChooser({initialLatitude}, {initialLongitude}, '{calTopoApiKey}', '{bingApiKey}')",
            true));
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
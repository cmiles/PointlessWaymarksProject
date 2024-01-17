using System.Text.Encodings.Web;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

// ReSharper disable StringLiteralTypo

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public static class WpfHtmlDocument
{
    public static List<(string fileName, string content)> CmsLeafletMapHtmlAndJs(string title, double initialLatitude,
        double initialLongitude, string cssStyleBlock)
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              {LeafletStandardHeaderContent(title, cssStyleBlock)}
                              <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                          </head>
                          <body onload="initialMapLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 98vh;"></div>
                          </body>
                          </html>
                          """;

        var javascriptString = LeafletMapJs(initialLatitude, initialLongitude);

        return [("Index.html", htmlString), ("CmsLeafletMap.js", javascriptString)];
    }

    public static List<LeafletLayerEntry> LeafletLayerList()
    {
        var layers = new List<LeafletLayerEntry>
        {
            new("openTopoMap", "OSM Topo", """
                                           
                                                           var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
                                                               maxNativeZoom: 17,
                                                               maxZoom: 24,
                                                               id: 'osmTopo',
                                                               attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
                                                           });
                                           """),
            new("tnmImageTopoMap", "TNM Image Topo", """

                                                     var tnmImageTopoMap =  L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}',
                                                             {
                                                                 maxNativeZoom: 16,
                                                                 maxZoom: 22,
                                                                 id: 'tnmImageTopo',
                                                                 attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
                                                             });
                                                     """),
            new("tnmTopoMap", "TNM Topo", """
                                          
                                                     var tnmTopoMap =  L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}',
                                                          {
                                                              maxNativeZoom: 16,
                                                              maxZoom: 22,
                                                              id: 'tnmTopo',
                                                              attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
                                                          });
                                          """)
        };

        return layers;
    }

    public static string LeafletMapJs(double initialLatitude, double initialLongitude)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $$"""
                        var mapLayers = [];
                        var map;

                        {{string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}}

                        function broadcastProgress(progress) {
                            console.log(progress);
                            window.chrome.webview.postMessage( { "messageType": "progress", "message": progress } );
                        }

                        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

                        function geoJsonLayerStyle(feature) {
                            //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0
                            var newStyle = {};
                        
                            if (feature.properties.hasOwnProperty("stroke")) newStyle.color = feature.properties["stroke"];
                            if (feature.properties.hasOwnProperty("stroke-width")) newStyle.weight = feature.properties["stroke-width"];
                            if (feature.properties.hasOwnProperty("stroke-opacity")) newStyle.opacity = feature.properties["stroke-opacity"];
                            if (feature.properties.hasOwnProperty("fill")) newStyle.fillColor = feature.properties["fill"];
                            if (feature.properties.hasOwnProperty("fill-opacity")) newStyle.fillOpacity = feature.properties["fill-opacity"];
                        
                            return newStyle;
                        }

                        function onEachMapGeoJsonFeature(feature, layer) {
                            if (feature.properties && feature.properties.title) {
                                layer.bindPopup(feature.properties.title);
                            }
                        }

                        function postGeoJsonDataHandler(e) {
                            if(mapLayer != null) map.removeLayer(mapLayer);
                        
                            let mapLayer = e.data;
                        
                            if(Object.keys(mapLayer).length === 0) return;
                        
                            map.flyToBounds([
                                [geoJsonData.Bounds.MinLatitude, geoJsonData.Bounds.MinLongitude],
                                [geoJsonData.Bounds.MaxLatitude, geoJsonData.Bounds.MaxLongitude]
                            ]);
                        
                            mapLayer = new L.geoJSON(geoJsonData.GeoJson, {
                                onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
                            });
                        
                            map.addLayer(mapLayer);
                        };

                        function initialMapLoad() {
                        
                          map = L.map('mainMap', {
                              center: { lat: {{initialLatitude}}, lng: {{initialLongitude}} },
                              zoom: 13,
                              layers: [{{string.Join(", ", layers.Select(x => x.LayerVariableName))}}],
                              doubleClickZoom: false,
                              closePopupOnClick: false
                          });
                        
                          var baseMaps = {
                              {{string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}}
                          };
                        
                          map.on('moveend', function(e) {
                              window.chrome.webview.postMessage( { "messageType": "mapBoundsChange", "bounds": map.getBounds() } );
                          });
                        
                          L.control.layers(baseMaps).addTo(map);
                        
                          window.chrome.webview.addEventListener('message', postGeoJsonDataHandler);
                          
                          window.chrome.webview.postMessage( { "messageType": "script-finished" } );
                        }
                        """;

        return htmlDoc;
    }

    public static string LeafletStandardHeaderContent(string title, string styleBlock)
    {
        return $"""

                <!doctype html>
                <html lang=en>
                <head>
                    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>{HtmlEncoder.Default.Encode(title)}</title>
                    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                    <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                    <style>{styleBlock}</style>
                </head>
                """;
    }

    public static void SetupCmsLeafletMapHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, string cssStyleBlock = "")
    {
        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.AddRange(CmsLeafletMapHtmlAndJs(title,
            initialLatitude, initialLongitude, cssStyleBlock));

        messenger.ToWebView.Enqueue(initialWebFilesMessage);

        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));
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
                            <link rel="stylesheet" href="https://[[VirtualDomain]]/pure.css" />
                            {{(string.IsNullOrWhiteSpace(styleBlock) ? """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""" : string.Empty)}}
                            {{(string.IsNullOrWhiteSpace(javascript) ? """<script src="https://[[VirtualDomain]]/customScript.js"></script>""" : string.Empty)}}
                            <style>{{styleBlock}}</style>
                        </head>
                        <body>
                            {{body}}
                            <script>
                                window.chrome.webview.postMessage( { "messageType": "script-finished" } );
                            </script>
                        </body>
                        </html>
                        """;

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.Add(("Index.html", htmlDoc));
        if (!string.IsNullOrWhiteSpace(minimalCss)) initialWebFilesMessage.Create.Add(("pure.css", minimalCss));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customScript.js", htmlDoc));

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
                            {{(string.IsNullOrWhiteSpace(styleBlock) ? """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""" : string.Empty)}}
                            {{(string.IsNullOrWhiteSpace(javascript) ? """<script src="https://[[VirtualDomain]]/customScript.js"></script>""" : string.Empty)}}
                            <style>{{styleBlock}}</style>
                        </head>
                        <body>
                            {{body}}
                            <script>
                                window.chrome.webview.postMessage( { "messageType": "script-finished" } );
                            </script>
                        </body>
                        </html>
                        """;

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.Add(("Index.html", htmlDoc));
        if (!string.IsNullOrWhiteSpace(pureCss)) initialWebFilesMessage.Create.Add(("pure.css", pureCss));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customScript.js", htmlDoc));

        messenger.ToWebView.Enqueue(initialWebFilesMessage);
        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));
    }

    public record LeafletLayerEntry(string LayerVariableName, string LayerName, string LayerDeclaration);
}
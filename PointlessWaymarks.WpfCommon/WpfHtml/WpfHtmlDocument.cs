using System.Text.Encodings.Web;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

// ReSharper disable StringLiteralTypo

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public static class WpfHtmlDocument
{
    public static FileBuilder CmsLeafletMapHtmlAndJs(string title, double initialLatitude,
        double initialLongitude, string styleBlock = "", string javascript = "")
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              {LeafletStandardHeaderContent(title)}
                              <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                              {(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""" )}
                              {(string.IsNullOrWhiteSpace(javascript) ? string.Empty: """<script src="https://[[VirtualDomain]]/customScript.js"></script>""" )}
                          </head>
                          <body onload="initialMapLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 96vh;"></div>
                          </body>
                          </html>
                          """;

        var initialWebFilesMessage = new FileBuilder();
        
        if (!string.IsNullOrWhiteSpace(styleBlock)) initialWebFilesMessage.Create.Add(("customStyle.css", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(("CmsLeafletMap.js", LeafletMapJs(initialLatitude, initialLongitude)));
        initialWebFilesMessage.Create.Add(("Index.html",htmlString));
        
        return initialWebFilesMessage;
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

                        {{string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}}

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
                        
                            if (feature.properties && (feature.properties.title || feature.properties.description)) {
                                let popupHtml = "";
                        
                                if (feature.properties.title) {
                                    popupHtml += feature.properties.title;
                                }
                        
                                if (feature.properties.description) {
                                    popupHtml += `<p style="text-align: center;">${feature.properties.description}</p>`;
                                }
                                
                                if(popupHtml !== "") layer.bindPopup(popupHtml, { autoClose: true });
                        
                                layer.on('click', function (e) {
                                    console.log(e);
                                    window.chrome.webview.postMessage({ "messageType": "featureClicked", "data": e.target.feature.properties }); });
                            }
                        }

                        function postGeoJsonDataHandler(e, clearCurrent, center) {
                        
                            if(clearCurrent) {
                                broadcastProgress('Clearing Map Layers');
                                if(Object.keys(mapLayers).length > 0) {
                                    mapLayers.forEach(item => map.removeLayer(item));
                                }
                                mapLayers = [];
                            }
                        
                            let mapData = e.data;
                        
                            if(Object.keys(mapData.GeoJsonLayers).length === 0) return;
                        
                            if(center) {
                                map.fitBounds([
                                    [mapData.Bounds.MinLatitude, mapData.Bounds.MinLongitude],
                                    [mapData.Bounds.MaxLatitude, mapData.Bounds.MaxLongitude]
                                ]);
                            }
                        
                            var newLayerCount = mapData.GeoJsonLayers.length;
                            var currentCount = 0;
                        
                            mapData.GeoJsonLayers.forEach(item => {
                                broadcastProgress(`Adding Layer ${++currentCount} of ${newLayerCount}`);
                                let newLayer = new L.geoJSON(item, {onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle});
                                mapLayers.push(newLayer);
                                map.addLayer(newLayer); });
                        }

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
                        
                          window.chrome.webview.addEventListener('message', function (e) {
                              
                              console.log(e);
                              
                              if(e.data.MessageType === 'NewFeatureCollection') postGeoJsonDataHandler(e, true, false);
                              
                              if(e.data.MessageType === 'NewFeatureCollectionAndCenter') postGeoJsonDataHandler(e, true, true);
                              
                              if(e.data.MessageType === 'AddFeatureCollection') postGeoJsonDataHandler(e, false, false);
                              
                              if(e.data.MessageType === 'CenterFeatureRequest') {
                                  console.log('Center Feature Request');
                                  map.eachLayer(function (l) {
                                      if (l.feature?.properties?.displayId === e.data.DisplayId) {
                                          console.log(`l.feature?.geometry?.type ${l.feature?.geometry?.type}`);
                                          
                                          if(l.feature?.geometry?.type === 'Point') {
                                              map.flyTo([l.feature.geometry.coordinates[1], l.feature.geometry.coordinates[0]]);
                                          }
                                          
                                          if(l.feature?.geometry?.type === 'LineString') {
                                              map.flyToBounds([[l.feature.bbox[1], l.feature.bbox[0]],
                                                  [l.feature.bbox[3], l.feature.bbox[2]]]);
                                          }
                                          l.openPopup();
                                      }
                                  })
                              }
                              
                              if(e.data.MessageType === 'ShowPopupsFor') {
                                  console.log(`Show Popups Request`);
                                  map.eachLayer(function (l) {
                                      if(!l.feature?.properties?.displayId) return;
                                      if (e.data.IdentifierList.includes(l.feature?.properties?.displayId)) {
                                          console.log(`opening popup for l.feature ${l.feature}`);
                                          l.openPopup();
                                      }
                                      else {
                                          console.log(`closing popup for l.feature ${l.feature}`);
                                          l.closePopup();
                                      }
                                      console.log(l);
                                  })
                              }
                              
                              if(e.data.MessageType === 'CenterCoordinateRequest') {
                                  console.log('Center Coordinate Request');
                                  map.flyTo([e.data.Latitude, e.data.Longitude]);
                              }
                              
                              if(e.data.MessageType === 'CenterBoundingBoxRequest') {
                                  console.log('Center Bounding Box Request');
                                  map.flyToBounds([[e.data.Bounds.MinLatitude, e.data.Bounds.MinLongitude], [e.data.Bounds.MaxLatitude, e.data.Bounds.MaxLongitude]]);
                              }
                          });
                          
                          console.log('scriptFinished');
                          window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
                        }
                        """;

        return htmlDoc;
    }

    public static string LeafletStandardHeaderContent(string title)
    {
        return $"""
                    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>{HtmlEncoder.Default.Encode(title)}</title>
                    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                    <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                """;
    }

    public static void SetupCmsLeafletMapHtmlAndJs(this IWebViewMessenger messenger, string title,
        double initialLatitude, double initialLongitude, string cssStyleBlock = "", string javascript = "")
    {
        var initialWebFilesMessage = CmsLeafletMapHtmlAndJs(title,
            initialLatitude, initialLongitude, cssStyleBlock, javascript);

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
                            <link rel="stylesheet" href="https://[[VirtualDomain]]/minimal.css" />
                            {{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty :  """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}
                            {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty :  """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}
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

        if (!string.IsNullOrWhiteSpace(minimalCss)) initialWebFilesMessage.Create.Add(("minimal.css", minimalCss));
        if (!string.IsNullOrWhiteSpace(styleBlock)) initialWebFilesMessage.Create.Add(("customStyle.js", htmlDoc));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customScript.js", htmlDoc));
        initialWebFilesMessage.Create.Add(("Index.html", htmlDoc));

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
                            {{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""" )}}
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

        if (!string.IsNullOrWhiteSpace(pureCss)) initialWebFilesMessage.Create.Add(("pure.css", pureCss));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customStyle.js", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript)) initialWebFilesMessage.Create.Add(("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(("Index.html", htmlDoc));

        messenger.ToWebView.Enqueue(initialWebFilesMessage);
        messenger.ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));
    }

    public record LeafletLayerEntry(string LayerVariableName, string LayerName, string LayerDeclaration);
}
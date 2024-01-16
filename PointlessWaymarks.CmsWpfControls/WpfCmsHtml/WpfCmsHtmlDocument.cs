using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class WpfCmsHtmlDocument
{
    public static List<(string fileName, string content)> CmsLeafletMapHtmlAndJs(string title, double initialLatitude,
        double initialLongitude)
    {
        var htmlString = $$"""
                           <!doctype html>
                           <html lang=en>
                           <head>
                               {{LeafletStandardHeaderContent(title)}}
                               <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                           </head>
                           <body onload="initialMapLoad();">
                                <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                   style="height: 98vh;"></div>
                           </body>
                           </html>
                           """;

        var javascriptString = CmsLeafletMapJs(initialLatitude, initialLongitude);

        return [("Index.html", htmlString), ("CmsLeafletMap.js", javascriptString)];
    }

    private static string CmsLeafletMapJs(double initialLatitude, double initialLongitude)
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
                            
                            window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
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
                        
                                if(popupHtml !== "") layer.bindPopup(popupHtml, { autoClose: false });
                        
                                layer.on('click', function (e) {
                                    console.log(e);
                                    window.chrome.webview.postMessage({ "messageType": "featureClicked", "data": e.target.feature.properties }); });
                            }
                        }

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
                        };
                        """;

        return htmlDoc;
    }

    public static List<(string fileName, string content)> CmsLeafletPointChooserMapHtmlAndJs(string title,
        double initialLatitude, double initialLongitude)
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              {LeafletStandardHeaderContent(title)}
                              <script src="https://[[VirtualDomain]]/CmsLeafletPointChooserMap.js"></script>
                          </head>
                          <body onload="initialMapLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 98vh;"></div>
                          </body>
                          </html>
                          """;

        var javascriptString = ToHtmlLeafletPointChooserJs(initialLatitude, initialLongitude);

        return [("Index.html", htmlString), ("CmsLeafletPointChooserMap.js", javascriptString)];
    }

    public static async Task<List<(string fileName, string content)>> CmsLeafletSpatialScriptHtmlAndJs(string body,
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

        return [("Index.html", htmlString), ("pointless-waymarks-spatial-common.js", spatialScript)];
    }

    public static List<WpfHtmlDocument.LeafletLayerEntry> LeafletLayerList()
    {
        var layers = new List<WpfHtmlDocument.LeafletLayerEntry>
        {
            new("openTopoMap", "OSM Topo", """
                                           
                                                           var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
                                                               maxNativeZoom: 17,
                                                               maxZoom: 24,
                                                               id: 'osmTopo',
                                                               attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
                                                           });
                                           """)
        };

        if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().CalTopoApiKey))
        {
            layers.Add(new WpfHtmlDocument.LeafletLayerEntry("calTopoTopoLayer", "CalTopo Topo", $$"""

                  var calTopoTopoLayer = L.tileLayer('http://caltopo.com/api/{accessToken}/wmts/tile/t/{z}/{x}/{y}.png', {
                          attribution: 'CalTopo',
                          maxNativeZoom: 16,
                          maxZoom: 24,
                          id: 'caltopoT',
                          accessToken: '{{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}}'
                      });
                  """));

            layers.Add(new WpfHtmlDocument.LeafletLayerEntry("calTopoFsLayer", "CalTopo FS", $$"""

                  var calTopoFsLayer = L.tileLayer('http://caltopo.com/api/{accessToken}/wmts/tile/f16a/{z}/{x}/{y}.png', {
                          attribution: 'CalTopo',
                          maxNativeZoom: 16,
                          maxZoom: 24,
                          id: 'caltopoF16a',
                          accessToken: '{{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}}'
                      });
                  """));
        }

        if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().BingApiKey))
        {
            layers.Add(new WpfHtmlDocument.LeafletLayerEntry("bingAerialTileLayer", "Bing Aerial", $$"""

                  var bingAerialTileLayer = L.tileLayer.bing({
                          bingMapsKey: '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}', // Required
                          imagerySet: 'AerialWithLabels',
                          maxZoom: 24
                      });
                  """));

            layers.Add(new WpfHtmlDocument.LeafletLayerEntry("bingRoadTileLayer", "Bing Roads", $$"""

                  var bingRoadTileLayer = L.tileLayer.bing({
                          bingMapsKey: '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}', // Required
                          imagerySet: 'RoadOnDemand',
                          maxZoom: 24
                      });
                  """));
        }

        layers.Add(new WpfHtmlDocument.LeafletLayerEntry("tnmImageTopoMap", "TNM Image Topo", """

            var tnmImageTopoMap =  L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}',
                    {
                        maxNativeZoom: 16,
                        maxZoom: 22,
                        id: 'tnmImageTopo',
                        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
                    });
            """));
        layers.Add(new WpfHtmlDocument.LeafletLayerEntry("tnmTopoMap", "TNM Topo", """
            
                       var tnmTopoMap =  L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}',
                            {
                                maxNativeZoom: 16,
                                maxZoom: 22,
                                id: 'tnmTopo',
                                attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
                            });
            """));

        return layers;
    }

    public static string LeafletStandardHeaderContent(string title)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        string bingScript;
        using (var embeddedAsStream = embeddedProvider.GetFileInfo("leaflet-bing-layer.js").CreateReadStream())
        {
            var reader = new StreamReader(embeddedAsStream);
            bingScript = reader.ReadToEnd();
        }

        return $"""
                    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>{HtmlEncoder.Default.Encode(title)}</title>
                    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                    <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                    <script>
                        {bingScript}
                    </script>
                """;
    }

    public static string ToHtmlLeafletPointChooserJs(double initialLatitude, double initialLongitude)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $$"""
                        var mapLayers = [];
                        var map;
                        {{string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}}

                        var geojsonMarkerOptions = {
                            radius: 8,
                            fillColor: "#ff7800",
                            color: "#000",
                            weight: 1,
                            opacity: 1,
                            fillOpacity: 0.8
                        };

                        function broadcastProgress(progress) {
                            console.log(progress);
                            window.chrome.webview.postMessage( { "messageType": "progress", "message": progress } );
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
                        
                            map.on('dblclick', function (e) {
                                console.log(e);
                                pointContentMarker.setLatLng(e.latlng);
                                window.chrome.webview.postMessage({ messageType: 'userSelectedLatitudeLongitudeChanged', latitude: e.latlng.lat, longitude: e.latlng.lng });
                            });
                                
                            var pointContentMarker = new L.marker([{{initialLatitude}},{{initialLongitude}}],{
                                draggable: true,
                                autoPan: true
                            }).addTo(map);
                                
                            pointContentMarker.on('dragend', function(e) {
                                console.log(e);
                                window.chrome.webview.postMessage({ messageType: 'userSelectedLatitudeLongitudeChanged', latitude: e.target._latlng.lat, longitude: e.target._latlng.lng });
                            });
                        
                            window.chrome.webview.addEventListener('message', function (e) {
                                
                                console.log(e);
                                
                                if(e.data.MessageType === 'NewFeatureCollection') postGeoJsonDataHandler(e, true, false);
                                
                                if(e.data.MessageType === 'NewFeatureCollectionAndCenter') postGeoJsonDataHandler(e, true, true);
                                
                                if(e.data.MessageType === 'AddFeatureCollection') postGeoJsonDataHandler(e, false, false);
                                
                                if(e.data.MessageType === 'CenterFeatureRequest') {
                                    broadcastProgress('Center Feature Request');
                                    
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
                                    broadcastProgress(`Show Popups Request`);
                                    
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
                                    broadcastProgress('Center Coordinate Request');
                                    map.flyTo([e.data.Latitude, e.data.Longitude]);
                                }
                                
                                if(e.data.MessageType === 'CenterBoundingBoxRequest') {
                                    broadcastProgress('Center Bounding Box Request');
                                    map.flyToBounds([[e.data.Bounds.MinLatitude, e.data.Bounds.MinLongitude], [e.data.Bounds.MaxLatitude, e.data.Bounds.MaxLongitude]]);
                                }
                                
                                if(e.data.MessageType === 'MoveUserLocationSelection') {
                                    broadcastProgress('Mover User Location Selection Request');
                                    pointContentMarker.setLatLng([e.data.Latitude,e.data.Longitude]);
                                    map.setView([e.data.Latitude,e.data.Longitude], map.getZoom());
                                } });
                                
                                window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
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
                        
                                    if(popupHtml !== "") layer.bindPopup(popupHtml, { autoClose: false });
                        
                                    layer.on('click', function (e) {
                                        console.log(e);
                                        window.chrome.webview.postMessage({ "messageType": "featureClicked", "data": e.target.feature.properties }); });
                                }
                            }
                        
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
                                    map.flyToBounds([
                                        [mapData.Bounds.MinLatitude, mapData.Bounds.MinLongitude],
                                        [mapData.Bounds.MaxLatitude, mapData.Bounds.MaxLongitude]
                                    ]);
                                }
                        
                                var newLayerCount = mapData.GeoJsonLayers.length;
                                var currentCount = 0;
                        
                                mapData.GeoJsonLayers.forEach(item => {
                                    broadcastProgress(`Adding Layer ${++currentCount} of ${newLayerCount}`);
                                    let newLayer = new L.geoJSON(item, {onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle, pointToLayer: function (feature, latlng) { return L.circleMarker(latlng, geojsonMarkerOptions) } });
                                    mapLayers.push(newLayer);
                                    map.addLayer(newLayer); });
                            };
                        """;

        return htmlDoc;
    }
}
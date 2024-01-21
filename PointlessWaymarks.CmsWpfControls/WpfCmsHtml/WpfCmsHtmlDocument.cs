using System.Text.Encodings.Web;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtmlResources;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class WpfCmsHtmlDocument
{
    public static string CmsLeafletLocationChooserJs(double initialLatitude, double initialLongitude)
    {
        var htmlDoc = $$"""
                        function initialMapLoad() {
                        
                            let [baseMaps, baseMapNames] = generateBaseMaps('{{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}}', '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}');
                        
                            map = L.map('mainMap', {
                                center: { lat: {{initialLatitude}}, lng: {{initialLongitude}} },
                                zoom: 13,
                                layers: baseMaps,
                                doubleClickZoom: false,
                                closePopupOnClick: false
                            });
                        
                            map.on('moveend', onMapMoveEnd);
                        
                            L.control.layers(baseMapNames).addTo(map);
                        
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
                                    let newLayer = new L.geoJSON(item, {onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle, pointToLayer: function (feature, latlng) { return L.circleMarker(latlng, pointCircleMarkerOrangeOptions) } });
                                    mapLayers.push(newLayer);
                                    map.addLayer(newLayer); });
                            };
                        """;

        return htmlDoc;
    }

    public static FileBuilder CmsLeafletMapHtmlAndJs(string title, double initialLatitude,
        double initialLongitude)
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              {LeafletStandardHeaderContent(title)}
                              <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                              <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                              <script src="https://[[VirtualDomain]]/CmsLeafletMap.js"></script>
                          </head>
                          <body onload="initialMapLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 98vh;"></div>
                          </body>
                          </html>
                          """;

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.Add(new FileBuilderCreate("leafletBingLayer.js",
            WpfHtmlResourcesHelper.LeafletBingLayerJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("localMapCommon.js",
            WpfHtmlResourcesHelper.LocalMapCommonJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("CmsLeafletMap.js",
            CmsLeafletMapJs(initialLatitude, initialLongitude)));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    private static string CmsLeafletMapJs(double initialLatitude, double initialLongitude)
    {
        var htmlDoc = $$"""
                        function initialMapLoad() {
                        
                           let [baseMaps, baseMapNames] = generateBaseMaps('{{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}}', '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}');
                        
                            map = L.map('mainMap', {
                                center: { lat: {{initialLatitude}}, lng: {{initialLongitude}} },
                                zoom: 13,
                                layers: baseMaps,
                                doubleClickZoom: false,
                                closePopupOnClick: false
                            });
                        
                            map.on('moveend', onMapMoveEnd);
                        
                            L.control.layers(baseMapNames).addTo(map);
                        
                            window.chrome.webview.addEventListener('message', processMapMessage);
                            
                            window.chrome.webview.postMessage( { "messageType": "scriptFinished" } );
                        }
                        """;

        return htmlDoc;
    }

    public static FileBuilder CmsLeafletPointChooserMapHtmlAndJs(string title,
        double initialLatitude, double initialLongitude)
    {
        var htmlString = $"""
                          <!doctype html>
                          <html lang=en>
                          <head>
                              {LeafletStandardHeaderContent(title)}
                              <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                              <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                              <script src="https://[[VirtualDomain]]/CmsLeafletPointChooserMap.js"></script>
                          </head>
                          <body onload="initialMapLoad();">
                               <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                  style="height: 98vh;"></div>
                          </body>
                          </html>
                          """;

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.Add(new FileBuilderCreate("leafletBingLayer.js",
            WpfHtmlResourcesHelper.LeafletBingLayerJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("localMapCommon.js",
            WpfHtmlResourcesHelper.LocalMapCommonJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("CmsLeafletPointChooserMap.js",
            CmsLeafletLocationChooserJs(initialLatitude, initialLongitude)));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

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
}
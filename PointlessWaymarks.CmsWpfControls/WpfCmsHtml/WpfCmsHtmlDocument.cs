using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class WpfCmsHtmlDocument
{
    public static string LeafletDocumentOpening(string title, string styleBlock)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        string bingScript;
        using (var embeddedAsStream = embeddedProvider.GetFileInfo("leaflet-bing-layer.js").CreateReadStream())
        {
            var reader = new StreamReader(embeddedAsStream);
            bingScript = reader.ReadToEnd();
        }

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
                    <script>
                        {bingScript}
                    </script>
                    <style>{styleBlock}</style>
                </head>
                """;
    }

    public static List<WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry> LeafletLayerList()
    {
        var layers = new List<WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry>
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
            layers.Add(new WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry("calTopoTopoLayer", "CalTopo Topo", $$"""
                  
                  var calTopoTopoLayer = L.tileLayer('http://caltopo.com/api/{accessToken}/wmts/tile/t/{z}/{x}/{y}.png', {
                          attribution: 'CalTopo',
                          maxNativeZoom: 16,
                          maxZoom: 24,
                          id: 'caltopoT',
                          accessToken: '{{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}}'
                      });
                  """));

            layers.Add(new WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry("calTopoFsLayer", "CalTopo FS", $$"""
                  
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
            layers.Add(new WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry("bingAerialTileLayer", "Bing Aerial", $$"""
                  
                  var bingAerialTileLayer = L.tileLayer.bing({
                          bingMapsKey: '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}', // Required
                          imagerySet: 'AerialWithLabels',
                          maxZoom: 24
                      });
                  """));

            layers.Add(new WpfCommon.WpfHtml.WpfHtmlDocument.LeafletLayerEntry("bingRoadTileLayer", "Bing Roads", $$"""
                  
                  var bingRoadTileLayer = L.tileLayer.bing({
                          bingMapsKey: '{{UserSettingsSingleton.CurrentSettings().BingApiKey}}', // Required
                          imagerySet: 'RoadOnDemand',
                          maxZoom: 24
                      });
                  """));
        }

        layers.Add(new("tnmImageTopoMap", "TNM Image Topo", """
                                                            
                                                            var tnmImageTopoMap =  L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}',
                                                                    {
                                                                        maxNativeZoom: 16,
                                                                        maxZoom: 22,
                                                                        id: 'tnmImageTopo',
                                                                        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
                                                                    });
                                                            """));
        layers.Add(new("tnmTopoMap", "TNM Topo", """
                                                 
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

    public static string ToHtmlLeafletMapDocument(string title, double initialLatitude, double initialLongitude,
        string styleBlock)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $$"""

                        {{LeafletDocumentOpening(title, styleBlock)}}
                        <body>
                             <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                style="height: 98vh;"></div>
                            <script>
                                {{string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}}
                        
                                var map = L.map('mainMap', {
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
                                    if(e.data.MessageType === 'NewFeatureCollection') postGeoJsonDataHandler(e);
                                    if(e.data.MessageType === 'CenterFeatureRequest') {
                                        console.log('Center Feature Request');
                                        map.eachLayer(function (l) {
                                            if (l.feature?.properties?.displayId === e.data.DisplayId) {
                                                console.log(`l.feature?.geometry?.type ${l.feature?.geometry?.type}`);
                                                if(l.feature?.geometry?.type === 'Point') {
                                                    map.flyTo([l.feature.geometry.coordinates[1], l.feature.geometry.coordinates[0]]);
                                                }
                                                if(l.feature?.geometry?.type === 'LineString') {
                                                    map.flyToBounds([[l.feature.bbox[1], l.feature.bbox[0]], [l.feature.bbox[3], l.feature.bbox[2]]]);
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
                        
                                var mapLayers = [];
                        
                                function postGeoJsonDataHandler(e) {
                                    if(Object.keys(mapLayers).length > 0) {
                                        mapLayers.forEach(item => map.removeLayer(item));
                                    }
                        
                                    mapLayers = [];
                        
                                    let mapData = e.data;
                        
                                    if(Object.keys(mapData.GeoJsonLayers).length === 0) return;
                        
                                    map.flyToBounds([
                                        [mapData.Bounds.MinLatitude, mapData.Bounds.MinLongitude],
                                        [mapData.Bounds.MaxLatitude, mapData.Bounds.MaxLongitude]
                                    ]);
                        
                                    mapData.GeoJsonLayers.forEach(item => {
                                        let newLayer = new L.geoJSON(item, {onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle});
                                        mapLayers.push(newLayer);
                                        map.addLayer(newLayer); });
                                };
                        
                                window.chrome.webview.postMessage( { "messageType": "script-finished" } );
                        
                            </script>
                        </body>
                        </html>
                        """;

        return htmlDoc;
    }

    public static async Task<string> ToHtmlLeafletPointDocument(string title, Guid? contentId, double initialLatitude,
        double initialLongitude,
        string styleBlock)
    {
        var db = await Db.Context();

        var otherPoints = (await db.PointContents.Where(x => x.ContentId != contentId).OrderBy(x => x.Slug)
            .AsNoTracking()
            .ToListAsync()).Select(x => new { x.Latitude, x.Longitude, x.Title }).ToList();

        var otherPointsJsonData = JsonSerializer.Serialize(otherPoints);

        var layers = LeafletLayerList();

        var htmlDoc = $$"""

                        {{LeafletDocumentOpening(title, styleBlock)}}
                        <body>
                             <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                style="height: 92vh;"></div>
                            <script>
                                {{string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}}
                        
                                var map = L.map('mainMap', {
                                    center: { lat: {{initialLatitude}}, lng: {{initialLongitude}} },
                                    zoom: 13,
                                    layers: [{{string.Join(", ", layers.Select(x => x.LayerVariableName))}}],
                                    doubleClickZoom: false
                                });
                        
                                var baseMaps = {
                                    {{string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}}
                                };
                        
                                L.control.layers(baseMaps).addTo(map);
                        
                                map.on('dblclick', function (e) {
                                    console.log(e);
                                    pointContentMarker.setLatLng(e.latlng);
                                    window.chrome.webview.postMessage(e.latlng.lat + ";" + e.latlng.lng);
                                });
                        
                                var pointContentMarker = new L.marker([{{initialLatitude}},{{initialLongitude}}],{
                                    draggable: true,
                                    autoPan: true
                                }).addTo(map);
                        
                                pointContentMarker.on('dragend', function(e) {
                                    console.log(e);
                                    window.chrome.webview.postMessage(e.target._latlng.lat + ";" + e.target._latlng.lng);
                                });
                        
                                const pointData = {{otherPointsJsonData}};
                        
                                for (let circlePoint of pointData) {
                                    let toAdd = L.circleMarker([circlePoint.Latitude, circlePoint.Longitude],
                                        40, { color: "blue", fillColor: "blue", fillOpacity: .5 });
                        
                                    const circlePopup = L.popup({ autoClose: false, autoPan: false })
                                        .setContent(`<p>${circlePoint.Title}</p>`);
                                    const boundCirclePopup = toAdd.bindPopup(circlePopup);
                        
                                    toAdd.addTo(map);
                                };
                        
                            </script>
                        </body>
                        </html>
                        """;

        return htmlDoc;
    }
}
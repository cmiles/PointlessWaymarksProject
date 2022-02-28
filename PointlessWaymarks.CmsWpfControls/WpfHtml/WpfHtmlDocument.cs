using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsWpfControls.WpfHtml;

public static class WpfHtmlDocument
{
    private static string LeafletDocumentOpening(string title, string styleBlock)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        string bingScript;
        using (var embeddedAsStream = embeddedProvider.GetFileInfo("leaflet-bing-layer.js").CreateReadStream())
        {
            var reader = new StreamReader(embeddedAsStream);
            bingScript = reader.ReadToEnd();
        }

        return $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.7.1/dist/leaflet.css""
       integrity=""sha512-xodZBNTC5n17Xt2atTPuE1HxjVMSvLVW9ocqUKLsCC5CXdbqCmblAshOMAS6/keqq/sMZMZ19scR4PsZChSR7A==""
       crossorigin=""""/>
    <script src=""https://unpkg.com/leaflet@1.7.1/dist/leaflet.js""
       integrity=""sha512-XQoYMqMTK8LvdxXYG3nZ448hOEQiglfqkJs1NOQV44cWnUrBc8PkAOcXy20w0vlaXaVUearIOBhiXZ5V3ynxwA==""
       crossorigin=""""></script>
    <script>
        {bingScript}
    </script>
    <style>{styleBlock}</style>
</head>";
    }

    private static List<LayerEntry> LeafletLayerList()
    {
        var layers = new List<LayerEntry>
        {
            new("openTopoMap", "OSM Topo", @"
        var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
            maxNativeZoom: 17,
            maxZoom: 24,
            id: 'osmTopo',
            attribution: 'Map data: &copy; <a href=""https://www.openstreetmap.org/copyright"">OpenStreetMap</a> contributors, <a href=""http://viewfinderpanoramas.org"">SRTM</a> | Map style: &copy; <a href=""https://opentopomap.org"">OpenTopoMap</a> (<a href=""https://creativecommons.org/licenses/by-sa/3.0/"">CC-BY-SA</a>)'
        });")
        };


        if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().CalTopoApiKey))
        {
            layers.Add(new LayerEntry("calTopoTopoLayer", "CalTopo Topo", $@"
                 var calTopoTopoLayer = L.tileLayer('http://caltopo.com/api/{{accessToken}}/wmts/tile/t/{{z}}/{{x}}/{{y}}.png', {{
            attribution: 'CalTopo',
            maxNativeZoom: 16,
            maxZoom: 24,
            id: 'caltopoT',
            accessToken: '{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}'
        }});"));

            layers.Add(new LayerEntry("calTopoFsLayer", "CalTopo FS", $@"
                 var calTopoFsLayer = L.tileLayer('http://caltopo.com/api/{{accessToken}}/wmts/tile/f16a/{{z}}/{{x}}/{{y}}.png', {{
            attribution: 'CalTopo',
            maxNativeZoom: 16,
            maxZoom: 24,
            id: 'caltopoF16a',
            accessToken: '{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}'
        }});"));
        }

        if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().BingApiKey))
        {
            layers.Add(new LayerEntry("bingAerialTileLayer", "Bing Aerial", $@"
                          var bingAerialTileLayer = L.tileLayer.bing({{
            bingMapsKey: '{UserSettingsSingleton.CurrentSettings().BingApiKey}', // Required
            imagerySet: 'AerialWithLabels',
            maxZoom: 24
        }});"));

            layers.Add(new LayerEntry("bingRoadTileLayer", "Bing Roads", $@"
                          var bingRoadTileLayer = L.tileLayer.bing({{
            bingMapsKey: '{UserSettingsSingleton.CurrentSettings().BingApiKey}', // Required
            imagerySet: 'RoadOnDemand',
            maxZoom: 24
        }});"));
        }

        return layers;
    }

    public static string ToHtmlDocument(this string body, string title, string styleBlock)
    {
        var spatialScript = FileManagement.SpatialScriptsAsString().Result;

        var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta charset=""utf-8"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{styleBlock}</style>
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.7.1/dist/leaflet.css""
       integrity=""sha512-xodZBNTC5n17Xt2atTPuE1HxjVMSvLVW9ocqUKLsCC5CXdbqCmblAshOMAS6/keqq/sMZMZ19scR4PsZChSR7A==""
       crossorigin=""""/>
    <script src=""https://unpkg.com/leaflet@1.7.1/dist/leaflet.js""
       integrity=""sha512-XQoYMqMTK8LvdxXYG3nZ448hOEQiglfqkJs1NOQV44cWnUrBc8PkAOcXy20w0vlaXaVUearIOBhiXZ5V3ynxwA==""
       crossorigin=""""></script>
    <script>
{spatialScript}
    </script>
</head>
<body>
    {body}
</body>
</html>";

        return htmlDoc;
    }

    public static string ToHtmlDocumentWithPureCss(this string body, string title, string styleBlock)
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        string pureCss;
        using (var embeddedAsStream = embeddedProvider.GetFileInfo("leaflet-bing-layer.js").CreateReadStream())
        {
            var reader = new StreamReader(embeddedAsStream);
            pureCss = reader.ReadToEnd();
        }

        var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta charset=""utf-8"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{pureCss}{styleBlock}</style>
</head>
<body>
    {body}
</body>
</html>";

        return htmlDoc;
    }

    public static string ToHtmlLeafletGeoJsonDocument(string title, double initialLatitude, double initialLongitude,
        string styleBlock)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $@"
{LeafletDocumentOpening(title, styleBlock)}
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 92vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.LayerVariableName))}],
            doubleClickZoom: false
        }});

        var baseMaps = {{
            {string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}
        }};

        L.control.layers(baseMaps).addTo(map);

        window.chrome.webview.addEventListener('message', postGeoJsonDataHandler);

        function geoJsonLayerStyle(feature) {{

            var newStyle = {{}};

            if (feature.properties.hasOwnProperty('stroke')) newStyle.color = feature.properties['stroke'];
            if (feature.properties.hasOwnProperty('stroke-width')) newStyle.weight = feature.properties['stroke-width'];
            if (feature.properties.hasOwnProperty('stroke-opacity')) newStyle.opacity = feature.properties['stroke-opacity'];
            if (feature.properties.hasOwnProperty('fill')) newStyle.fillColor = feature.properties['fill'];
            if (feature.properties.hasOwnProperty('fill-opacity')) newStyle.fillOpacity = feature.properties['fill-opacity'];

            return newStyle;
        }}

        function onEachMapGeoJsonFeature(feature, layer) {{
            if (feature.properties && feature.properties.title) {{
                layer.bindPopup(feature.properties.title);
            }}
        }}


        var geoMapLayer;

        function postGeoJsonDataHandler(e) {{
            if(geoMapLayer != null) map.removeLayer(geoMapLayer);

            let geoJsonData = e.data;

            if(Object.keys(geoJsonData).length === 0) return;

            map.flyToBounds([
                [geoJsonData.Bounds.InitialViewBoundsMinLatitude, geoJsonData.Bounds.InitialViewBoundsMinLongitude],
                [geoJsonData.Bounds.InitialViewBoundsMaxLatitude, geoJsonData.Bounds.InitialViewBoundsMaxLongitude]
            ]);

            geoMapLayer = new L.geoJSON(geoJsonData.GeoJson, {{
                onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle
            }});

            map.addLayer(geoMapLayer);
        }};

        window.chrome.webview.postMessage('script_finished');

    </script>
</body>
</html>";

        return htmlDoc;
    }

    public static string ToHtmlLeafletLineDocument(string title, double initialLatitude, double initialLongitude,
        string styleBlock)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $@"
{LeafletDocumentOpening(title, styleBlock)}
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 92vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.LayerVariableName))}],
            doubleClickZoom: false
        }});

        var baseMaps = {{
            {string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}
        }};

        L.control.layers(baseMaps).addTo(map);

        window.chrome.webview.addEventListener('message', postGeoJsonDataHandler);

        function onEachMapGeoJsonFeature(feature, layer) {{
            if (feature.properties && feature.properties.PopupContent) {{
                layer.bindPopup(feature.properties.PopupContent);
            }}
            if (feature.properties && feature.properties.popupContent) {{
                layer.bindPopup(feature.properties.PopupContent);
            }}
        }}

        var geoMapLayer;

        function postGeoJsonDataHandler(e) {{
            if(geoMapLayer != null) map.removeLayer(geoMapLayer);

            let lineData = e.data;

            if(Object.keys(lineData).length === 0) return;

            map.flyToBounds([
                [lineData.Bounds.InitialViewBoundsMinLatitude, lineData.Bounds.InitialViewBoundsMinLongitude],
                [lineData.Bounds.InitialViewBoundsMaxLatitude, lineData.Bounds.InitialViewBoundsMaxLongitude]
            ]);

            geoMapLayer = new L.geoJSON(lineData.GeoJson, {{
                onEachFeature: onEachMapGeoJsonFeature
            }});

            map.addLayer(geoMapLayer);
        }};

        window.chrome.webview.postMessage('script_finished');

    </script>
</body>
</html>";

        return htmlDoc;
    }

    public static string ToHtmlLeafletMapDocument(string title, double initialLatitude, double initialLongitude,
        string styleBlock)
    {
        var layers = LeafletLayerList();

        var htmlDoc = $@"
{LeafletDocumentOpening(title, styleBlock)}
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 92vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.LayerVariableName))}],
            doubleClickZoom: false
        }});

        var baseMaps = {{
            {string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}
        }};

        L.control.layers(baseMaps).addTo(map);

        window.chrome.webview.addEventListener('message', postGeoJsonDataHandler);

        function onEachMapGeoJsonFeature(feature, layer) {{

            if (feature.properties && (feature.properties.title || feature.properties.description)) {{
                let popupHtml = """";

                if (feature.properties.title) {{
                    popupHtml += feature.properties.title;
                }}

                if (feature.properties.description) {{
                    popupHtml += `<p>${{feature.properties.description}}</p>`;
                }}

                if(popupHtml !== """") layer.bindPopup(popupHtml);
            }}
        }}

        function geoJsonLayerStyle(feature) {{
            //see https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0
            var newStyle = {{}};

            if (feature.properties.hasOwnProperty(""stroke"")) newStyle.color = feature.properties[""stroke""];
            if (feature.properties.hasOwnProperty(""stroke-width"")) newStyle.weight = feature.properties[""stroke-width""];
            if (feature.properties.hasOwnProperty(""stroke-opacity"")) newStyle.opacity = feature.properties[""stroke-opacity""];
            if (feature.properties.hasOwnProperty(""fill"")) newStyle.fillColor = feature.properties[""fill""];
            if (feature.properties.hasOwnProperty(""fill-opacity"")) newStyle.fillOpacity = feature.properties[""fill-opacity""];

            return newStyle;
        }}

        var mapLayers = [];

        function postGeoJsonDataHandler(e) {{
            if(Object.keys(mapLayers).length > 0) {{ 
                mapLayers.forEach(item => map.removeLayer(item));
            }}

            mapLayers = [];

            let mapData = e.data;

            if(Object.keys(mapData.GeoJsonLayers).length === 0) return;

            map.flyToBounds([
                [mapData.Bounds.InitialViewBoundsMinLatitude, mapData.Bounds.InitialViewBoundsMinLongitude],
                [mapData.Bounds.InitialViewBoundsMaxLatitude, mapData.Bounds.InitialViewBoundsMaxLongitude]
            ]);

            mapData.GeoJsonLayers.forEach(item => {{
                let newLayer = new L.geoJSON(item, {{onEachFeature: onEachMapGeoJsonFeature, style: geoJsonLayerStyle}});
                mapLayers.push(newLayer);
                map.addLayer(newLayer); }});
        }};

        window.chrome.webview.postMessage('script_finished');

    </script>
</body>
</html>";

        return htmlDoc;
    }

    public static async Task<string> ToHtmlLeafletPointDocument(string title, Guid? contentId, double initialLatitude, double initialLongitude,
        string styleBlock)
    {
        var db = await Db.Context();

        var otherPoints = (await db.PointContents.Where(x => x.ContentId != contentId).OrderBy(x => x.Slug).AsNoTracking()
            .ToListAsync()).Select(x => new {x.Latitude, x.Longitude, x.Title}).ToList();

        var otherPointsJsonData = JsonSerializer.Serialize(otherPoints);

        var layers = LeafletLayerList();

        var htmlDoc = $@"
{LeafletDocumentOpening(title, styleBlock)}
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 92vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.LayerVariableName))}],
            doubleClickZoom: false
        }});

        var baseMaps = {{
            {string.Join(",", layers.Select(x => $"\"{x.LayerName}\" : {x.LayerVariableName}"))}
        }};

        L.control.layers(baseMaps).addTo(map);

        map.on('dblclick', function (e) {{
            console.log(e);
            pointContentMarker.setLatLng(e.latlng);
            window.chrome.webview.postMessage(e.latlng.lat + "";"" + e.latlng.lng);

        }});

        var pointContentMarker = new L.marker([{initialLatitude},{initialLongitude}],{{
            draggable: true,
            autoPan: true
        }}).addTo(map);

        pointContentMarker.on('dragend', function(e) {{
            console.log(e);
            window.chrome.webview.postMessage(e.target._latlng.lat + "";"" + e.target._latlng.lng);
        }});

        const pointData = {otherPointsJsonData};

        for (let circlePoint of pointData) {{
            let toAdd = L.circleMarker([circlePoint.Latitude, circlePoint.Longitude],
                40, {{ color: ""blue"", fillColor: ""blue"", fillOpacity: .5 }});

            const circlePopup = L.popup({{ autoClose: false, autoPan: false }})
                .setContent(`<p>${{circlePoint.Title}}</p>`);
            const boundCirclePopup = toAdd.bindPopup(circlePopup);

            toAdd.addTo(map);
        }};

    </script>
</body>
</html>";

        return htmlDoc;
    }


    private record LayerEntry(string LayerVariableName, string LayerName, string LayerDeclaration);
}
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using ABI.Windows.Devices.Bluetooth.Advertisement;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public static class WpfHtmlDocument
{
    public static string LeafletDocumentBasicOpening(string title, string styleBlock)
    {
        return $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.css"" integrity=""sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI="" crossorigin="""" />
    <script src=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.js"" integrity=""sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM="" crossorigin=""""></script>
    <style>{styleBlock}</style>
</head>";
    }

    public static List<LeafletLayerEntry> LeafletBasicLayerList()
    {
        var layers = new List<LeafletLayerEntry>
        {
            new("openTopoMap", "OSM Topo", @"
        var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
            maxNativeZoom: 17,
            maxZoom: 24,
            id: 'osmTopo',
            attribution: 'Map data: &copy; <a href=""https://www.openstreetmap.org/copyright"">OpenStreetMap</a> contributors, <a href=""http://viewfinderpanoramas.org"">SRTM</a> | Map style: &copy; <a href=""https://opentopomap.org"">OpenTopoMap</a> (<a href=""https://creativecommons.org/licenses/by-sa/3.0/"">CC-BY-SA</a>)'
        });")
        };

        return layers;
    }

    public static string ToHtmlDocumentWithLeaflet(this string body, string title, string styleBlock)
    {
        var spatialScript = FileSystemHelpers.SpatialScriptsAsString().Result;

        var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta charset=""utf-8"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{styleBlock}</style>
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.css"" integrity=""sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI="" crossorigin="""" />
    <script src=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.js"" integrity=""sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM="" crossorigin=""""></script>
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

    public static async Task<string> ToHtmlDocumentWithPureCss(this string body, string title, string styleBlock)
    {
        var pureCss = await FileSystemHelpers.PureCssAsString();

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

    public static string ToHtmlLeafletBasicGeoJsonDocument(string title, double initialLatitude,
        double initialLongitude, string styleBlock)
    {
        var layers = LeafletBasicLayerList();

        var htmlDoc = $@"
{LeafletDocumentBasicOpening(title, styleBlock)}
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 97.5vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.LayerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.LayerVariableName))}],
            doubleClickZoom: false
        }});

        map.panTo({{ lat: {initialLatitude}, lng: {initialLongitude} }});

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

        window.chrome.webview.postMessage( {{ ""messageType"": ""script-finished"" }} );

    </script>
</body>
</html>";

        return htmlDoc;
    }

    public record LeafletLayerEntry(string LayerVariableName, string LayerName, string LayerDeclaration);
}
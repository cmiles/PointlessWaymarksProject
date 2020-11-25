using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.Extensions.FileProviders;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.WpfHtml
{
    public static class WpfHtmlDocument
    {
        public static string ToHtmlDocument(this string body, string title, string styleBlock)
        {
            var htmlDoc = $@"
<!doctype html>
<html lang=en>
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta charset=""utf-8"">
    <title>{HtmlEncoder.Default.Encode(title)}</title>
    <style>{styleBlock}</style>
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

        public static string ToHtmlLeafletDocument(string title, double initialLatitude, double initialLongitude,
            string styleBlock)
        {
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

            string bingScript;
            using (var embeddedAsStream = embeddedProvider.GetFileInfo("leaflet-bing-layer.js").CreateReadStream())
            {
                var reader = new StreamReader(embeddedAsStream);
                bingScript = reader.ReadToEnd();
            }

            var layers = new List<(string layerVariableName, string layerName, string layerDeclaration)>
            {
                ("openTopoMap", "OSM Topo", @"
        var openTopoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
            maxZoom: 17,
            id: 'osmTopo',
            attribution: 'Map data: &copy; <a href=""https://www.openstreetmap.org/copyright"">OpenStreetMap</a> contributors, <a href=""http://viewfinderpanoramas.org"">SRTM</a> | Map style: &copy; <a href=""https://opentopomap.org"">OpenTopoMap</a> (<a href=""https://creativecommons.org/licenses/by-sa/3.0/"">CC-BY-SA</a>)'
        });")
            };


            if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().CalTopoApiKey))
            {
                layers.Add(("calTopoTopoLayer", "CalTopo Topo", $@"
                 var calTopoTopoLayer = L.tileLayer('http://caltopo.com/api/{{accessToken}}/wmts/tile/t/{{z}}/{{x}}/{{y}}.png', {{
            attribution: 'CalTopo',
            maxZoom: 16,
            id: 'caltopoT',
            accessToken: '{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}'
        }});"));

                if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().CalTopoApiKey))
                    layers.Add(("calTopoFsLayer", "CalTopo FS", $@"
                 var calTopoFsLayer = L.tileLayer('http://caltopo.com/api/{{accessToken}}/wmts/tile/f16a/{{z}}/{{x}}/{{y}}.png', {{
            attribution: 'CalTopo',
            maxZoom: 16,
            id: 'caltopoF16a',
            accessToken: '{UserSettingsSingleton.CurrentSettings().CalTopoApiKey}'
        }});"));
            }

            if (!string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().BingApiKey))
            {
                layers.Add(("bingAerialTileLayer", "Bing Aerial", $@"
                          var bingAerialTileLayer = L.tileLayer.bing({{
            bingMapsKey: '{UserSettingsSingleton.CurrentSettings().BingApiKey}', // Required
            imagerySet: 'AerialWithLabels',
        }});"));

                layers.Add(("bingRoadTileLayer", "Bing Roads", $@"
                          var bingRoadTileLayer = L.tileLayer.bing({{
            bingMapsKey: '{UserSettingsSingleton.CurrentSettings().BingApiKey}', // Required
            imagerySet: 'RoadOnDemand',
        }});"));
            }

            var htmlDoc = $@"
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
</head>
<body>
     <div id=""mainMap"" class=""leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag""
        style=""height: 92vh;""></div>
    <script>
        {string.Join($"{Environment.NewLine}", layers.Select(x => x.layerDeclaration))}

        var map = L.map('mainMap', {{
            center: {{ lat: {initialLatitude}, lng: {initialLongitude} }},
            zoom: 13,
            layers: [{string.Join(", ", layers.Select(x => x.layerVariableName))}],
            doubleClickZoom: false
        }});

        var baseMaps = {{
            {string.Join(",", layers.Select(x => $"\"{x.layerName}\" : {x.layerVariableName}"))}
        }};

        L.control.layers(baseMaps).addTo(map);

        map.on('dblclick', function (e) {{
            console.log(e);
            pointContentMarker.setLatLng(e.latlng);
            window.external.notify(e.latlng.lat + "";"" + e.latlng.lng);

        }});

        var pointContentMarker = new L.marker([{initialLatitude},{initialLongitude}],{{
            draggable: true,
            autoPan: true
        }}).addTo(map);

        pointContentMarker.on('dragend', function(e) {{
            console.log(e);
            window.chrome.webview.postMessage(e.target._latlng.lat + "";"" + e.target._latlng.lng);
        }});

    </script>
</body>
</html>";

            return htmlDoc;
        }
    }
}
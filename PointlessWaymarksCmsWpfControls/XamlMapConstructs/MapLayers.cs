using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MapControl;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    public class MapLayers : INotifyPropertyChanged
    {
        private readonly Dictionary<string, UIElement> mapLayers;

        private string currentMapLayerName = "CalTopo Topo Layer";

        public MapLayers()
        {
            // Bing Maps TileLayers with tile URLs retrieved from the Imagery Metadata Service
            // (see http://msdn.microsoft.com/en-us/library/ff701716.aspx).
            // A Bing Maps API Key (see http://msdn.microsoft.com/en-us/library/ff428642.aspx) is required
            // for using these layers and must be assigned to the static BingMapsTileLayer.ApiKey property.

            mapLayers = CreateMapLayers();

            MapLayerNames.AddRange(
                new List<string> {"Thunderforest Outdoor", "OpenStreetMap", "OpenStreetMap TOPO WMS"});
        }

        public UIElement CurrentMapLayer => mapLayers[currentMapLayerName];

        public string CurrentMapLayerName
        {
            get => currentMapLayerName;
            set
            {
                currentMapLayerName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentMapLayerName)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentMapLayer)));
            }
        }

        public List<string> MapLayerNames { get; } = new List<string>();

        public UIElement SeamarksLayer => mapLayers["Seamarks"];

        private Dictionary<string, UIElement> CreateMapLayers()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var toReturn = new Dictionary<string, UIElement>();

            if (!string.IsNullOrWhiteSpace(settings.CalTopoApiKey))
            {
                toReturn.Add("CalTopo Topo Layer",
                    new WmsImageLayer
                    {
                        Description = "© CalTopo",
                        ServiceUri = new Uri($"http://caltopo.com/api/{settings.CalTopoApiKey}/wms?"),
                        Layers = "t"
                    });

                toReturn.Add("CalTopo Forest Service",
                    new WmsImageLayer
                    {
                        Description = "© CalTopo",
                        ServiceUri = new Uri($"http://caltopo.com/api/{settings.CalTopoApiKey}/wms?"),
                        Layers = "f16a"
                    });

                MapLayerNames.AddRange(new List<string> {"CalTopo Topo Layer", "CalTopo Forest Service"});
            }

            if (!string.IsNullOrWhiteSpace(settings.BingApiKey))
            {
                //9HG41G
                //BingMapsTileLayer.ApiKey = "";
                BingMapsTileLayer.ApiKey = settings.BingApiKey;

                toReturn.Add("Bing Maps Road",
                    new BingMapsTileLayer
                    {
                        SourceName = "Bing Maps Road",
                        Description = "© [Microsoft](http://www.bing.com/maps/)",
                        Mode = BingMapsTileLayer.MapMode.Road
                    });
                toReturn.Add("Bing Maps Aerial",
                    new BingMapsTileLayer
                    {
                        SourceName = "Bing Maps Aerial",
                        Description = "© [Microsoft](http://www.bing.com/maps/)",
                        Mode = BingMapsTileLayer.MapMode.Aerial,
                        MapForeground = new SolidColorBrush(Colors.White),
                        MapBackground = new SolidColorBrush(Colors.Black)
                    });
                toReturn.Add("Bing Maps Aerial with Labels",
                    new BingMapsTileLayer
                    {
                        SourceName = "Bing Maps Hybrid",
                        Description = "© [Microsoft](http://www.bing.com/maps/)",
                        Mode = BingMapsTileLayer.MapMode.AerialWithLabels,
                        MapForeground = new SolidColorBrush(Colors.White),
                        MapBackground = new SolidColorBrush(Colors.Black)
                    });

                MapLayerNames.AddRange(new List<string>
                {
                    "Bing Maps Road", "Bing Maps Aerial", "Bing Maps Aerial with Labels"
                });
            }

            toReturn.Add("Thunderforest Outdoor",
                new MapTileLayer
                {
                    SourceName = "Thunderforest",
                    Description = "© Thunderforest(https://manage.thunderforest.com/)",
                    TileSource = new TileSource
                    {
                        UriFormat =
                            "https://tile.thunderforest.com/outdoors/{z}/{x}/{y}.png?apikey=d76f52e2451c4e4698de41b28c89dff5"
                    },
                    MaxZoomLevel = 19
                });

            toReturn.Add("OpenStreetMap", MapTileLayer.OpenStreetMapTileLayer);

            toReturn.Add("OpenStreetMap German",
                new MapTileLayer
                {
                    SourceName = "OpenStreetMap German",
                    Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)",
                    TileSource = new TileSource
                    {
                        UriFormat = "http://{c}.tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png"
                    },
                    MaxZoomLevel = 19
                });
            toReturn.Add("Stamen Terrain",
                new MapTileLayer
                {
                    SourceName = "Stamen Terrain",
                    Description =
                        "Map tiles by [Stamen Design](http://stamen.com/), under [CC BY 3.0](http://creativecommons.org/licenses/by/3.0)\nData by [OpenStreetMap](http://openstreetmap.org/), under [ODbL](http://www.openstreetmap.org/copyright)",
                    TileSource = new TileSource {UriFormat = "http://tile.stamen.com/terrain/{z}/{x}/{y}.png"},
                    MaxZoomLevel = 17
                });
            toReturn.Add("Stamen Toner Light",
                new MapTileLayer
                {
                    SourceName = "Stamen Toner Light",
                    Description =
                        "Map tiles by [Stamen Design](http://stamen.com/), under [CC BY 3.0](http://creativecommons.org/licenses/by/3.0)\nData by [OpenStreetMap](http://openstreetmap.org/), under [ODbL](http://www.openstreetmap.org/copyright)",
                    TileSource = new TileSource {UriFormat = "http://tile.stamen.com/toner-lite/{z}/{x}/{y}.png"},
                    MaxZoomLevel = 18
                });
            toReturn.Add("Seamarks",
                new MapTileLayer
                {
                    SourceName = "OpenSeaMap",
                    TileSource = new TileSource {UriFormat = "http://tiles.openseamap.org/seamark/{z}/{x}/{y}.png"},
                    MinZoomLevel = 9,
                    MaxZoomLevel = 18
                });

            toReturn.Add("OpenStreetMap WMS",
                new WmsImageLayer
                {
                    Description =
                        "© [terrestris GmbH & Co. KG](http://ows.terrestris.de/)\nData © [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)",
                    ServiceUri = new Uri("http://ows.terrestris.de/osm/service"),
                    Layers = "OSM-WMS"
                });
            toReturn.Add("OpenStreetMap TOPO WMS",
                new WmsImageLayer
                {
                    Description =
                        "© [terrestris GmbH & Co. KG](http://ows.terrestris.de/)\nData © [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)",
                    ServiceUri = new Uri("http://ows.terrestris.de/osm/service"),
                    Layers = "TOPO-OSM-WMS"
                });
            toReturn.Add("SevenCs ChartServer",
                new WmsImageLayer
                {
                    Description = "© [SevenCs GmbH](http://www.sevencs.com)",
                    ServiceUri = new Uri("http://chartserver4.sevencs.com:8080"),
                    Layers = "ENC",
                    MaxBoundingBoxWidth = 360
                });

            return toReturn;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MapControl;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class StandardMapControl
    {
        public StandardMapControl()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        public StandardMapViewModel Model()
        {
            return (StandardMapViewModel) DataContext;
        }

        private void MapItemTouchDown(object sender, TouchEventArgs e)
        {
            var mapItem = (MapItem) sender;
            mapItem.IsSelected = !mapItem.IsSelected;

            if (e.Source is ISelectable baseObject) baseObject.IsSelected = mapItem.IsSelected;

            e.Handled = true;
        }

        private void MapManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.001;
        }

        private void MapMouseLeave(object sender, MouseEventArgs e)
        {
            mouseLocation.Text = string.Empty;
        }

        private void MapMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //map.ZoomMap(e.GetPosition(map), Math.Floor(map.ZoomLevel + 1.5));
                //map.ZoomToBounds(new BoundingBox(53, 7, 54, 9));
                //map.TargetCenter = map.ViewportPointToLocation(e.GetPosition(map));
                var location = map.ViewportPointToLocation(e.GetPosition(map));
                Model().CreateLocation(new Point(location.Longitude, location.Latitude));
            }
        }

        private void MapMouseMove(object sender, MouseEventArgs e)
        {
            var location = map.ViewportPointToLocation(e.GetPosition(map));
            var latitude = (int) Math.Round(location.Latitude * 60000d);
            var longitude = (int) Math.Round(Location.NormalizeLongitude(location.Longitude) * 60000d);
            var latHemisphere = 'N';
            var lonHemisphere = 'E';

            if (latitude < 0)
            {
                latitude = -latitude;
                latHemisphere = 'S';
            }

            if (longitude < 0)
            {
                longitude = -longitude;
                lonHemisphere = 'W';
            }

            mouseLocation.Text = string.Format(CultureInfo.InvariantCulture,
                "{0}  {1:00} {2:00.000}\n{3} {4:000} {5:00.000}", latHemisphere, latitude / 60000,
                latitude % 60000 / 1000d, lonHemisphere, longitude / 60000, longitude % 60000 / 1000d);
        }

        private void MapMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //map.ZoomMap(e.GetPosition(map), Math.Ceiling(map.ZoomLevel - 1.5));
            }
        }

        private void ModelOnListPointSelectionRequest(object sender, List<MapDisplayPoint> e)
        {
            MapPointsItemControl.SelectItems(x =>
            {
                if (e == null || !e.Any()) return false;
                if (!(x is MapDisplayPoint point)) return false;

                if (e.Last() == point) Model().MapCenter = point.Location;
                return e.Contains(point);
            });
        }

        private void ModelOnListPolylineSelectionRequest(object sender, List<MapDisplayPolyline> e)
        {
            MapPolylineItemControl.SelectItems(x =>
            {
                if (e == null || !e.Any()) return false;
                if (!(x is MapDisplayPolyline line)) return false;

                if (e.Last() == line && line.Locations.Count > 0) Model().MapCenter = line.Locations[0];
                return e.Contains(line);
            });
        }

        private void ModelOnListPushpinSelectionRequest(object sender, List<MapDisplayPoint> e)
        {
            MapPushPinsItemControl.SelectItems(x =>
            {
                if (!(x is MapDisplayPoint point)) return false;

                if (e.Last() == point) Model().MapCenter = point.Location;
                return e.Contains(point);
            });
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is StandardMapViewModel model)) return;

            model.MapPolylineSelectionRequest += ModelOnListPolylineSelectionRequest;
            model.MapPointSelectionRequest += ModelOnListPointSelectionRequest;
            model.MapPushpinSelectionRequest += ModelOnListPushpinSelectionRequest;
        }

        private void SeamarksChecked(object sender, RoutedEventArgs e)
        {
            map.Children.Insert(map.Children.IndexOf(mapGraticule),
                ((StandardMapViewModel) DataContext).MapLayers.SeamarksLayer);
        }

        private void SeamarksUnchecked(object sender, RoutedEventArgs e)
        {
            map.Children.Remove(((StandardMapViewModel) DataContext).MapLayers.SeamarksLayer);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using MapControl;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    public class StandardMapViewModel : INotifyPropertyChanged
    {
        private Location _mapCenter = new Location(53.5, 8.2);

        private StatusControlContext _statusContext = new StatusControlContext();

        public StandardMapViewModel(StatusControlContext statusContext)
        {
            StatusContext = statusContext;

            PointsMapSelectionChangedCommand = new Command<IList>(MapsPointsSelectionChanged);
            PolylinesMapSelectionChangedCommand = new Command<IList>(MapsPolylineSelectionChanged);

            ClearAllSelectionsCommand = new Command(() =>
            {
                MapsPointsSelectionChanged(null);
                MapsPolylineSelectionChanged(null);
            });
            ClearPointsSelectionCommand = new Command(() => MapsPointsSelectionChanged(null));
            ClearPolylinesSelectionCommand = new Command(() => MapsPolylineSelectionChanged(null));
        }

        public Command ClearAllSelectionsCommand { get; set; }

        public Command ClearPointsSelectionCommand { get; set; }
        public Command ClearPolylinesSelectionCommand { get; set; }

        public Location MapCenter
        {
            get => _mapCenter;
            set
            {
                if (Equals(value, _mapCenter)) return;
                _mapCenter = value;
                OnPropertyChanged();
            }
        }

        public MapLayers MapLayers { get; } = new MapLayers();
        public ObservableCollection<MapDisplayPoint> Points { get; } = new ObservableCollection<MapDisplayPoint>();
        public Command<IList> PointsMapSelectionChangedCommand { get; set; }

        public ObservableCollection<MapDisplayPolyline> Polylines { get; } =
            new ObservableCollection<MapDisplayPolyline>();

        public Command<IList> PolylinesMapSelectionChangedCommand { get; set; }
        public ObservableCollection<MapDisplayPoint> Pushpins { get; } = new ObservableCollection<MapDisplayPoint>();

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public async void CreateLocation(Point getPosition)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newMapPoint = new MapDisplayPoint
            {
                Id = Guid.NewGuid(),
                Location = new MapLocationZ(getPosition.Y, getPosition.X, null),
                Name = "New Point",
                ExtendedDescription = "Waypoint"
            };

            await ThreadSwitcher.ResumeForegroundAsync();

            Points.Add(newMapPoint);
        }

        private void MapsPointsSelectionChanged(IList pointList)
        {
            if (pointList == null || pointList.Count == 0)
            {
                ListPointSelectionRequest?.Invoke(this, new List<MapDisplayPoint>());
                return;
            }

            var finalList = new List<MapDisplayPoint>();

            foreach (var loopList in pointList)
            {
                var mapPoint = loopList as MapDisplayPoint;

                finalList.Add(mapPoint);
            }

            ListPointSelectionRequest?.Invoke(this, finalList);
        }

        private void MapsPolylineSelectionChanged(IList pointList)
        {
            if (pointList == null || pointList.Count == 0)
            {
                ListPolylineSelectionRequest?.Invoke(this, new List<MapDisplayPolyline>());
                return;
            }

            var finalList = new List<MapDisplayPolyline>();

            foreach (var loopList in pointList)
            {
                var mapPoint = loopList as MapDisplayPolyline;

                finalList.Add(mapPoint);
            }

            ListPolylineSelectionRequest?.Invoke(this, finalList);
        }

        public void OnMapPointSelectionRequest(object sender, List<MapDisplayPoint> toRequest)
        {
            MapPointSelectionRequest?.Invoke(sender, toRequest);
        }

        public void OnMapPolylineSelectionRequest(object sender, List<MapDisplayPolyline> toRequest)
        {
            MapPolylineSelectionRequest?.Invoke(sender, toRequest);
        }

        public void OnMapPushPinSelectionRequest(object sender, List<MapDisplayPoint> toRequest)
        {
            MapPushpinSelectionRequest?.Invoke(sender, toRequest);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<List<MapDisplayPoint>> ListPointSelectionRequest;

        public event EventHandler<List<MapDisplayPolyline>> ListPolylineSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> ListPushpinSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> MapPointSelectionRequest;

        public event EventHandler<List<MapDisplayPolyline>> MapPolylineSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> MapPushpinSelectionRequest;
    }
}
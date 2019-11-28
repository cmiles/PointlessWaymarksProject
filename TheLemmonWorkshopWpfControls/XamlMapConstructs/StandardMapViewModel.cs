using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using MapControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.XamlMapConstructs
{
    public class StandardMapViewModel : INotifyPropertyChanged
    {
        private Location _mapCenter = new Location(53.5, 8.2);

        private ControlStatusViewModel _statusContext = new ControlStatusViewModel();

        public StandardMapViewModel(ControlStatusViewModel statusContext)
        {
            StatusContext = statusContext;

            PointsMapSelectionChangedCommand = new RelayCommand<IList>(MapsPointsSelectionChanged);
            PolylinesMapSelectionChangedCommand = new RelayCommand<IList>(MapsPolylineSelectionChanged);

            ClearAllSelectionsCommand = new RelayCommand(() =>
            {
                MapsPointsSelectionChanged(null);
                MapsPolylineSelectionChanged(null);
            });
            ClearPointsSelectionCommand = new RelayCommand(() => MapsPointsSelectionChanged(null));
            ClearPolylinesSelectionCommand = new RelayCommand(() => MapsPolylineSelectionChanged(null));
        }

        public event EventHandler<List<MapDisplayPoint>> ListPointSelectionRequest;

        public event EventHandler<List<MapDisplayPolyline>> ListPolylineSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> ListPushpinSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> MapPointSelectionRequest;

        public event EventHandler<List<MapDisplayPolyline>> MapPolylineSelectionRequest;

        public event EventHandler<List<MapDisplayPoint>> MapPushpinSelectionRequest;

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand ClearAllSelectionsCommand { get; set; }

        public RelayCommand ClearPointsSelectionCommand { get; set; }
        public RelayCommand ClearPolylinesSelectionCommand { get; set; }

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
        public RelayCommand<IList> PointsMapSelectionChangedCommand { get; set; }

        public ObservableCollection<MapDisplayPolyline> Polylines { get; } =
            new ObservableCollection<MapDisplayPolyline>();

        public RelayCommand<IList> PolylinesMapSelectionChangedCommand { get; set; }
        public ObservableCollection<MapDisplayPoint> Pushpins { get; } = new ObservableCollection<MapDisplayPoint>();

        public ControlStatusViewModel StatusContext
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
    }
}
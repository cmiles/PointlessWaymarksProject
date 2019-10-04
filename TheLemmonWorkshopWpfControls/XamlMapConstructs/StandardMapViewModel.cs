using JetBrains.Annotations;
using MapControl;
using System;
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
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public ObservableCollection<MapDisplayPolyline> Polylines { get; } = new ObservableCollection<MapDisplayPolyline>();

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
                Location = new MapLocationM(getPosition.Y, getPosition.X, null)
            };

            await ThreadSwitcher.ResumeForegroundAsync();

            Points.Add(newMapPoint);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
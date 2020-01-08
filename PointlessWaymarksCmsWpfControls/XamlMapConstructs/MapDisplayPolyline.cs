using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MapControl;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    public class MapDisplayPolyline : INotifyPropertyChanged, ISelectable
    {
        private string _extendedDescription;
        private Guid _id;
        private bool _isSelected;
        private LocationCollection _locations;
        private string _name;

        public string ExtendedDescription
        {
            get => _extendedDescription;
            set
            {
                if (value == _extendedDescription) return;
                _extendedDescription = value;
                OnPropertyChanged();
            }
        }

        public Guid Id
        {
            get => _id;
            set
            {
                if (value.Equals(_id)) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public LocationCollection Locations
        {
            get => _locations;
            set
            {
                if (Equals(value, _locations)) return;
                _locations = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
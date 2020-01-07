using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    public class MapDisplayPoint : INotifyPropertyChanged, ISelectable
    {
        private string _extendedDescription;

        private Guid _id;

        private bool _isSelected;

        private MapLocationZ _location;

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

        public MapLocationZ Location
        {
            get => _location;
            set
            {
                if (Equals(value, _location)) return;
                _location = value;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
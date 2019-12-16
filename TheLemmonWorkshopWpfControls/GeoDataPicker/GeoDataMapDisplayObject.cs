using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    public class MapDisplayObject : INotifyPropertyChanged
    {
        private string _displayName;
        private Guid _id;
        private bool _isProtectedFromClearing;

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (value == _displayName) return;
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public string ExtendedDescription { get; set; }

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

        public bool IsProtectedFromClearing
        {
            get => _isProtectedFromClearing;
            set
            {
                if (value == _isProtectedFromClearing) return;
                _isProtectedFromClearing = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
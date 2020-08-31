using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class StringChangeTrackingProperty : INotifyPropertyChanged, IHasChanges
    {
        private bool _hasChanges;
        private string _referenceValue;
        private string _userValue;

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public string ReferenceValue
        {
            get => _referenceValue;
            set
            {
                if (value == _referenceValue) return;
                _referenceValue = value;
                OnPropertyChanged();
            }
        }

        public string UserValue
        {
            get => _userValue;
            set
            {
                if (value == _userValue) return;
                _userValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }
    }
}
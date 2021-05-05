using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListListItem : INotifyPropertyChanged, ISelectedTextTracker
    {
        private PhotoContent _dbEntry;
        private CurrentSelectedTextTracker _selectedTextTracker = new();
        private string _smallImageUrl;

        public PhotoContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string SmallImageUrl
        {
            get => _smallImageUrl;
            set
            {
                if (value == _smallImageUrl) return;
                _smallImageUrl = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CurrentSelectedTextTracker SelectedTextTracker
        {
            get => _selectedTextTracker;
            set
            {
                if (Equals(value, _selectedTextTracker)) return;
                _selectedTextTracker = value;
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
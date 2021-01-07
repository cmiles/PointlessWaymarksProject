using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsWpfControls.FileList
{
    public class FileListListItem : INotifyPropertyChanged
    {
        private FileContent _dbEntry;
        private string _smallImageUrl;

        public FileContent DbEntry
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
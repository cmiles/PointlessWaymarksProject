using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;

namespace TheLemmonWorkshopWpfControls.PhotoList
{
    public class PhotoListListItem : INotifyPropertyChanged
    {
        private PhotoContent _dbEntry;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
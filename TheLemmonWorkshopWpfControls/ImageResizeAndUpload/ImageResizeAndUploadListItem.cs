using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace TheLemmonWorkshopWpfControls.ImageResizeAndUpload
{
    public class ImageResizeAndUploadListItem : INotifyPropertyChanged
    {
        private FileInfo _fileItem;
        private bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged();
            }
        }

        public FileInfo FileItem
        {
            get => _fileItem;
            set
            {
                if (Equals(value, _fileItem)) return;
                _fileItem = value;
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
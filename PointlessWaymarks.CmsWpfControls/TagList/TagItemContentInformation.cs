using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.TagList
{
    public class TagItemContentInformation : INotifyPropertyChanged
    {
        private Guid _contentId;
        private string _contentType;
        private string _tags;
        private string _title;

        public Guid ContentId
        {
            get => _contentId;
            set
            {
                if (value.Equals(_contentId)) return;
                _contentId = value;
                OnPropertyChanged();
            }
        }

        public string ContentType
        {
            get => _contentType;
            set
            {
                if (value == _contentType) return;
                _contentType = value;
                OnPropertyChanged();
            }
        }

        public string Tags
        {
            get => _tags;
            set
            {
                if (value == _tags) return;
                _tags = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
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
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace TheLemmonWorkshopWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged
    {
        private string _altText;
        private string _aperture;
        private string _baseFileName;
        private string _camera;
        private string _currentUpdateBy;
        private string _description;
        private Guid _fingerprint;
        private int _id;
        private DateTime? _lastUpdatedOn;
        private string _lens;
        private string _pageCreatedBy;
        private DateTime _pageCreatedOn;
        private DateTime _pageLastUpdateBy;
        private DateTime _pageLastUpdateOn;
        private string _photoCreatedBy;
        private DateTime _photoCreatedOn;
        private FileInfo _selectedFile;
        private string _shutterSpeed;
        private string _slug;
        private string _title;
        private string _updatedBy;
        private string _updateNotes;
        private string _updateNotesFormat;

        public string Slug
        {
            get => _slug;
            set
            {
                if (value == _slug) return;
                _slug = value;
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

        public string AltText
        {
            get => _altText;
            set
            {
                if (value == _altText) return;
                _altText = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public string PhotoCreatedBy
        {
            get => _photoCreatedBy;
            set
            {
                if (value == _photoCreatedBy) return;
                _photoCreatedBy = value;
                OnPropertyChanged();
            }
        }

        public string Camera
        {
            get => _camera;
            set
            {
                if (value == _camera) return;
                _camera = value;
                OnPropertyChanged();
            }
        }

        public string Lens
        {
            get => _lens;
            set
            {
                if (value == _lens) return;
                _lens = value;
                OnPropertyChanged();
            }
        }

        public string Aperture
        {
            get => _aperture;
            set
            {
                if (value == _aperture) return;
                _aperture = value;
                OnPropertyChanged();
            }
        }

        public string ShutterSpeed
        {
            get => _shutterSpeed;
            set
            {
                if (value == _shutterSpeed) return;
                _shutterSpeed = value;
                OnPropertyChanged();
            }
        }

        public string BaseFileName
        {
            get => _baseFileName;
            set
            {
                if (value == _baseFileName) return;
                _baseFileName = value;
                OnPropertyChanged();
            }
        }

        public string CurrentUpdateBy
        {
            get => _currentUpdateBy;
            set
            {
                if (value == _currentUpdateBy) return;
                _currentUpdateBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime PhotoCreatedOn
        {
            get => _photoCreatedOn;
            set
            {
                if (value.Equals(_photoCreatedOn)) return;
                _photoCreatedOn = value;
                OnPropertyChanged();
            }
        }

        public string PageCreatedBy
        {
            get => _pageCreatedBy;
            set
            {
                if (value == _pageCreatedBy) return;
                _pageCreatedBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime PageCreatedOn
        {
            get => _pageCreatedOn;
            set
            {
                if (value.Equals(_pageCreatedOn)) return;
                _pageCreatedOn = value;
                OnPropertyChanged();
            }
        }

        public DateTime PageLastUpdateOn
        {
            get => _pageLastUpdateOn;
            set
            {
                if (value.Equals(_pageLastUpdateOn)) return;
                _pageLastUpdateOn = value;
                OnPropertyChanged();
            }
        }

        public DateTime PageLastUpdateBy
        {
            get => _pageLastUpdateBy;
            set
            {
                if (value.Equals(_pageLastUpdateBy)) return;
                _pageLastUpdateBy = value;
                OnPropertyChanged();
            }
        }

        public Guid Fingerprint
        {
            get => _fingerprint;
            set
            {
                if (value.Equals(_fingerprint)) return;
                _fingerprint = value;
                OnPropertyChanged();
            }
        }

        public int Id
        {
            get => _id;
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string UpdatedBy
        {
            get => _updatedBy;
            set
            {
                if (value == _updatedBy) return;
                _updatedBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastUpdatedOn
        {
            get => _lastUpdatedOn;
            set
            {
                if (Nullable.Equals(value, _lastUpdatedOn)) return;
                _lastUpdatedOn = value;
                OnPropertyChanged();
            }
        }

        public string UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (value == _updateNotes) return;
                _updateNotes = value;
                OnPropertyChanged();
            }
        }

        public string UpdateNotesFormat
        {
            get => _updateNotesFormat;
            set
            {
                if (value == _updateNotesFormat) return;
                _updateNotesFormat = value;
                OnPropertyChanged();
            }
        }

        public FileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (Equals(value, _selectedFile)) return;
                _selectedFile = value;
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
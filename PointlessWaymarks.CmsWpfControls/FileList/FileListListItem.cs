using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList
{
    public class FileListListItem : IContentListItem, IContentListSmallImage
    {
        private FileContent _dbEntry;
        private FileListItemActions _itemActions;
        private CurrentSelectedTextTracker _selectedTextTracker = new();

        private bool _showType;
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

        public FileListItemActions ItemActions
        {
            get => _itemActions;
            set
            {
                if (Equals(value, _itemActions)) return;
                _itemActions = value;
                OnPropertyChanged();
            }
        }

        public bool ShowType
        {
            get => _showType;
            set
            {
                if (value == _showType) return;
                _showType = value;
                OnPropertyChanged();
            }
        }

        public Guid? ContentId()
        {
            return DbEntry?.ContentId;
        }

        public IContentCommon Content()
        {
            return DbEntry;
        }

        public string DefaultBracketCode()
        {
            if (DbEntry?.ContentId == null || ItemActions == null) return string.Empty;
            return DbEntry.MainPicture != null
                ? @$"{BracketCodeFileImage.Create(DbEntry)}"
                : @$"{BracketCodeFiles.Create(DbEntry)}";
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
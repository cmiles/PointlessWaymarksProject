using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList
{
    public class MapComponentListListItem : IContentListItem
    {
        private MapComponent _dbEntry;
        private MapComponentListItemActions _itemActions;
        private CurrentSelectedTextTracker _selectedTextTracker = new();

        public MapComponent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public MapComponentListItemActions ItemActions
        {
            get => _itemActions;
            set
            {
                if (Equals(value, _itemActions)) return;
                _itemActions = value;
                OnPropertyChanged();
            }
        }

        public Guid? ContentId()
        {
            return DbEntry?.ContentId;
        }

        public IContentCommon Content()
        {
            if (DbEntry == null) return null;

            return new ContentCommonShell
            {
                Summary = DbEntry.Summary,
                Title = DbEntry.Title,
                ContentId = DbEntry.ContentId,
                ContentVersion = DbEntry.ContentVersion,
                Id = DbEntry.Id,
                CreatedBy = DbEntry.CreatedBy,
                CreatedOn = DbEntry.CreatedOn,
                LastUpdatedBy = DbEntry.LastUpdatedBy,
                LastUpdatedOn = DbEntry.LastUpdatedOn
            };
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
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList
{
    public class MapComponentListListItem : IContentListItem
    {
        private MapComponent _dbEntry;
        private MapComponentContentActions _itemActions;
        private CurrentSelectedTextTracker _selectedTextTracker = new();

        private bool _showType;

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

        public MapComponentContentActions ItemActions
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

        public Guid? ContentId()
        {
            return DbEntry?.ContentId;
        }

        public string DefaultBracketCode()
        {
            return ItemActions.DefaultBracketCode(DbEntry);
        }

        public async Task DefaultBracketCodeToClipboard()
        {
            await ItemActions.DefaultBracketCodeToClipboard(DbEntry);
        }

        public async Task Delete()
        {
            await ItemActions.Delete(DbEntry);
        }

        public async Task Edit()
        {
            await ItemActions.Edit(DbEntry);
        }

        public async Task ExtractNewLinks()
        {
            await ItemActions.ExtractNewLinks(DbEntry);
        }

        public async Task GenerateHtml()
        {
            await ItemActions.GenerateHtml(DbEntry);
        }

        public async Task OpenUrl()
        {
            await ItemActions.OpenUrl(DbEntry);
        }

        public async Task ViewHistory()
        {
            await ItemActions.ViewHistory(DbEntry);
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
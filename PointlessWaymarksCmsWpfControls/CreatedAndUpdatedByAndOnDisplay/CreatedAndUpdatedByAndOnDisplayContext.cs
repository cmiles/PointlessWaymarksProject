using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay
{
    public class CreatedAndUpdatedByAndOnDisplayContext : INotifyPropertyChanged
    {
        private string _createdAndUpdatedByAndOn;
        private string _createdBy;
        private DateTime? _createdOn;
        private ICreatedAndLastUpdateOnAndBy _dbEntry;
        private bool _showCreatedByEditor;
        private bool _showUpdatedByEditor;
        private string _updatedBy;
        private DateTime? _updatedOn;

        public CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext,
            ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public string CreatedAndUpdatedByAndOn
        {
            get => _createdAndUpdatedByAndOn;
            set
            {
                if (value == _createdAndUpdatedByAndOn) return;
                _createdAndUpdatedByAndOn = value;
                OnPropertyChanged();
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (value == _createdBy) return;
                _createdBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime? CreatedOn
        {
            get => _createdOn;
            set
            {
                if (Nullable.Equals(value, _createdOn)) return;
                _createdOn = value;
                OnPropertyChanged();
            }
        }

        public ICreatedAndLastUpdateOnAndBy DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public bool ShowCreatedByEditor
        {
            get => _showCreatedByEditor;
            set
            {
                if (value == _showCreatedByEditor) return;
                _showCreatedByEditor = value;
                OnPropertyChanged();
            }
        }

        public bool ShowUpdatedByEditor
        {
            get => _showUpdatedByEditor;
            set
            {
                if (value == _showUpdatedByEditor) return;
                _showUpdatedByEditor = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext { get; set; }

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

        public DateTime? UpdatedOn
        {
            get => _updatedOn;
            set
            {
                if (Nullable.Equals(value, _updatedOn)) return;
                _updatedOn = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData(ICreatedAndLastUpdateOnAndBy toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;
            CreatedBy = toLoad?.CreatedBy ?? string.Empty;
            UpdatedBy = toLoad?.LastUpdatedBy ?? string.Empty;

            //If this is a 'first update' go ahead and fill in the Created by as the updated by, this
            //is realistically just a trade off, better for most common workflow - potential mistake
            //if trading off created/updated authors since you are not 'forcing' an entry
            if (!string.IsNullOrWhiteSpace(CreatedBy) && string.IsNullOrWhiteSpace(UpdatedBy)) UpdatedBy = CreatedBy;

            CreatedOn = toLoad?.CreatedOn;
            UpdatedOn = toLoad?.LastUpdatedOn;

            var newStringParts = new List<string>();

            CreatedAndUpdatedByAndOn = string.Empty;
            if (DbEntry == null)
            {
                CreatedAndUpdatedByAndOn = "New Entry";
                ShowCreatedByEditor = true;
                ShowUpdatedByEditor = false;
                return;
            }

            ShowCreatedByEditor = false;
            ShowUpdatedByEditor = true;

            if (!string.IsNullOrWhiteSpace(DbEntry.CreatedBy))
                newStringParts.Add($"Created By {DbEntry.CreatedBy.Trim()}");
            else
                newStringParts.Add("Created");
            newStringParts.Add($"On {DbEntry.CreatedOn:g}");

            if (!string.IsNullOrWhiteSpace(DbEntry.LastUpdatedBy))
                newStringParts.Add($"Last Update By {DbEntry.LastUpdatedBy.Trim()}");
            else if (DbEntry.LastUpdatedOn != null) newStringParts.Add("Last Update");

            if (DbEntry.LastUpdatedOn != null) newStringParts.Add($"On {DbEntry.LastUpdatedOn:g}");

            CreatedAndUpdatedByAndOn = string.Join(" ", newStringParts);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null && string.IsNullOrWhiteSpace(CreatedBy))
                return (false, "Created by can not be blank for a new entry.");

            if (DbEntry != null && string.IsNullOrWhiteSpace(UpdatedBy))
                return (false, "Updated by can not be blank when updating an entry");

            return (true, string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
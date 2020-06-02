using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay
{
    public class CreatedAndUpdatedByAndOnDisplayContext : INotifyPropertyChanged
    {
        private string _createdAndUpdatedByAndOn;
        private string _createdBy = string.Empty;
        private DateTime? _createdOn;
        private ICreatedAndLastUpdateOnAndBy _dbEntry;
        private bool _showCreatedByEditor;
        private bool _showUpdatedByEditor;
        private string _updatedBy = string.Empty;
        private DateTime? _updatedOn;
        private bool _createdByHasChanges;
        private bool _updatedHasChanges;
        private bool _isNewEntry;

        public CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext,
            ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod
            CreatedByHasChanges = CreatedBy.TrimNullSafe() != StringHelper.TrimNullSafe(DbEntry?.CreatedBy);
            UpdatedHasChanges = UpdatedBy.TrimNullSafe() != StringHelper.TrimNullSafe(DbEntry?.LastUpdatedBy);
            // ReSharper restore InvokeAsExtensionMethod
        }

        public bool UpdatedHasChanges
        {
            get => _updatedHasChanges;
            set
            {
                if (value == _updatedHasChanges) return;
                _updatedHasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool CreatedByHasChanges
        {
            get => _createdByHasChanges;
            set
            {
                if (value == _createdByHasChanges) return;
                _createdByHasChanges = value;
                OnPropertyChanged();
            }
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

        public bool IsNewEntry
        {
            get => _isNewEntry;
            set
            {
                if (value == _isNewEntry) return;
                _isNewEntry = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData(ICreatedAndLastUpdateOnAndBy toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            IsNewEntry = false;

            if (toLoad == null)
            {
                IsNewEntry = true;
            }
            else if (((IContentId) DbEntry).Id < 1)
            {
                IsNewEntry = true;
            }

            CreatedBy = string.IsNullOrWhiteSpace(toLoad?.CreatedBy)
                ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
                : DbEntry.CreatedBy;
            UpdatedBy = toLoad?.LastUpdatedBy ?? string.Empty;

            //If this is a 'first update' go ahead and fill in the Created by as the updated by, this
            //is realistically just a trade off, better for most common workflow - potential mistake
            //if trading off created/updated authors since you are not 'forcing' an entry
            if (!IsNewEntry && string.IsNullOrWhiteSpace(UpdatedBy)) UpdatedBy = CreatedBy;

            CreatedOn = toLoad?.CreatedOn;
            UpdatedOn = toLoad?.LastUpdatedOn;

            var newStringParts = new List<string>();

            CreatedAndUpdatedByAndOn = string.Empty;

            if (IsNewEntry)
            {
                CreatedAndUpdatedByAndOn = "New Entry";
                ShowCreatedByEditor = true;
                ShowUpdatedByEditor = false;
                return;
            }

            ShowCreatedByEditor = false;
            ShowUpdatedByEditor = true;

            newStringParts.Add(!string.IsNullOrWhiteSpace(DbEntry.CreatedBy)
                ? $"Created By {DbEntry.CreatedBy.Trim()}"
                : "Created");

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
            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }

        public async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (IsNewEntry && string.IsNullOrWhiteSpace(CreatedBy))
                return (false, "Created by can not be blank for a new entry.");

            if (!IsNewEntry && string.IsNullOrWhiteSpace(UpdatedBy))
                return (false, "Updated by can not be blank when updating an entry");

            return (true, string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay
{
    public class CreatedAndUpdatedByAndOnDisplayContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private string _createdAndUpdatedByAndOn;
        private StringDataEntryContext _createdByEntry;
        private DateTime? _createdOn;
        private ICreatedAndLastUpdateOnAndBy _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private bool _isNewEntry;
        private bool _showCreatedByEditor;
        private bool _showUpdatedByEditor;
        private StringDataEntryContext _updatedByEntry;
        private DateTime? _updatedOn;

        private CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
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

        public StringDataEntryContext CreatedByEntry
        {
            get => _createdByEntry;
            set
            {
                if (Equals(value, _createdByEntry)) return;
                _createdByEntry = value;
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

        public StringDataEntryContext UpdatedByEntry
        {
            get => _updatedByEntry;
            set
            {
                if (Equals(value, _updatedByEntry)) return;
                _updatedByEntry = value;
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

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<CreatedAndUpdatedByAndOnDisplayContext> CreateInstance(
            StatusControlContext statusContext, ICreatedAndLastUpdateOnAndBy dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newInstance = new CreatedAndUpdatedByAndOnDisplayContext(statusContext);
            await newInstance.LoadData(dbEntry);

            return newInstance;
        }

        public async Task LoadData(ICreatedAndLastUpdateOnAndBy toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            IsNewEntry = false;

            if (toLoad == null)
                IsNewEntry = true;
            else if (((IContentId) DbEntry).Id < 1) IsNewEntry = true;

            CreatedByEntry = StringDataEntryContext.CreateInstance();
            CreatedByEntry.ValidationFunctions = new List<Func<string, IsValid>>
            {
                CommonContentValidation.ValidateCreatedBy
            };
            CreatedByEntry.Title = "Created By";
            CreatedByEntry.HelpText = "Created By Name";
            CreatedByEntry.ReferenceValue = string.IsNullOrWhiteSpace(toLoad?.CreatedBy)
                ? string.Empty
                : DbEntry.CreatedBy;
            CreatedByEntry.UserValue = string.IsNullOrWhiteSpace(toLoad?.CreatedBy)
                ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
                : DbEntry.CreatedBy;


            UpdatedByEntry = StringDataEntryContext.CreateInstance();
            UpdatedByEntry.ValidationFunctions = new List<Func<string, IsValid>> {ValidateUpdatedBy};
            UpdatedByEntry.Title = "Updated By";
            UpdatedByEntry.HelpText = "Last Updated By Name";
            UpdatedByEntry.ReferenceValue = toLoad?.LastUpdatedBy ?? string.Empty;
            UpdatedByEntry.UserValue = toLoad?.LastUpdatedBy ?? string.Empty;

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);

            //If this is a 'first update' go ahead and fill in the Created by as the updated by, this
            //is realistically just a trade off, better for most common workflow - potential mistake
            //if trading off created/updated authors since you are not 'forcing' an entry
            if (!IsNewEntry && string.IsNullOrWhiteSpace(UpdatedByEntry.UserValue))
            {
                UpdatedByEntry.ReferenceValue = CreatedByEntry.UserValue;
                UpdatedByEntry.UserValue = CreatedByEntry.UserValue;
            }

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

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }


        public IsValid ValidateUpdatedBy(string updatedBy)
        {
            if (!IsNewEntry && string.IsNullOrWhiteSpace(updatedBy))
                return new IsValid(false, "Updated by can not be blank when updating an entry");

            return new IsValid(true, string.Empty);
        }
    }
}
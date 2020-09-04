using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.ContentFormat;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class CreatePointDetailNoteOnlyEditorContext<T> : IHasChanges, IHasValidationIssues, INotifyPropertyChanged,
        ICreatePointDetail where T : new()
    {
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PointDetail _dbEntry;
        private T _detailData;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private StringDataEntryContext _noteEditor;
        private ContentFormatChooserContext _noteFormatEditor;
        private StatusControlContext _statusContext;
        private string _typeIdentifier;

        public CreatedAndUpdatedByAndOnDisplayContext CreatedUpdatedDisplay
        {
            get => _createdUpdatedDisplay;
            set
            {
                if (Equals(value, _createdUpdatedDisplay)) return;
                _createdUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public PointDetail DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public T DetailData
        {
            get => _detailData;
            set
            {
                if (Equals(value, _detailData)) return;
                _detailData = value;
                OnPropertyChanged();
            }
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

        public StringDataEntryContext NoteEditor
        {
            get => _noteEditor;
            set
            {
                if (Equals(value, _noteEditor)) return;
                _noteEditor = value;
                OnPropertyChanged();
            }
        }

        public ContentFormatChooserContext NoteFormatEditor
        {
            get => _noteFormatEditor;
            set
            {
                if (Equals(value, _noteFormatEditor)) return;
                _noteFormatEditor = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public string TypeIdentifier
        {
            get => _typeIdentifier;
            set
            {
                if (value == _typeIdentifier) return;
                _typeIdentifier = value;
                OnPropertyChanged();
            }
        }

        public PointDetail CurrentPointDetail()
        {
            var newEntry = new PointDetail();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.DataType = TypeIdentifier;

            var detailData = new T();
            ((dynamic) DetailData).Notes = NoteEditor.UserValue.TrimNullToEmpty();
            ((dynamic) DetailData).NotesContentFormat = NoteFormatEditor.SelectedContentFormatAsString;

            Db.DefaultPropertyCleanup(detailData);

            newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

            return newEntry;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        }

        public async Task LoadData(PointDetail toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PointDetail
            {
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                DataType = ((dynamic) DetailData).DataTypeIdentifier,
            };


            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);

            if (!string.IsNullOrWhiteSpace(DbEntry.StructuredDataAsJson))
                DetailData = JsonSerializer.Deserialize<T>(DbEntry.StructuredDataAsJson);

            DetailData ??= new T();
            ((dynamic) DetailData).NotesContentFormat = UserSettingsUtilities.DefaultContentFormatChoice();

            NoteEditor = new StringDataEntryContext
            {
                Title = "Notes",
                HelpText = "Notes",
                ReferenceValue = ((dynamic) DetailData).Notes ?? string.Empty,
                UserValue = ((dynamic) DetailData).Notes.TrimNullToEmpty()
            };

            NoteFormatEditor =
                new ContentFormatChooserContext(StatusContext)
                {
                    InitialValue = ((dynamic) DetailData).NotesContentFormat
                };
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate();
        }

        public string SaveDataToJson()
        {
            throw new NotImplementedException();
        }
    }
}
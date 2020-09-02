using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailModels;
using PointlessWaymarksCmsWpfControls.ContentFormat;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailPeakEditorContext : INotifyPropertyChanged
    {
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PointDetail _dbEntry;
        private Peak _detailData;
        private bool _hasChanges;
        private StringDataEntryContext _noteEditor;
        private ContentFormatChooserContext _noteFormatEditor;
        private StatusControlContext _statusContext;

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

        public Peak DetailData
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        }

        public async Task LoadData(PointDetail toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PointDetail
            {
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                DataType = Peak.DataTypeIdentifier,
            };

            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);

            if (!string.IsNullOrWhiteSpace(DbEntry.StructuredDataAsJson))
                DetailData = JsonSerializer.Deserialize<Peak>(DbEntry.StructuredDataAsJson);

            DetailData ??= new Peak {NotesContentFormat = UserSettingsUtilities.DefaultContentFormatChoice()};

            NoteEditor = new StringDataEntryContext
            {
                Title = "Notes",
                HelpText = "Notes for the Peak",
                ReferenceValue = DetailData.Notes ?? string.Empty,
                UserValue = DetailData.Notes.TrimNullToEmpty()
            };

            NoteFormatEditor =
                new ContentFormatChooserContext(StatusContext) {InitialValue = DetailData.NotesContentFormat};
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate();
        }
    }
}
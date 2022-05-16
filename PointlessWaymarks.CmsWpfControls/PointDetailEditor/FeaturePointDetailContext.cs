using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

internal class FeaturePointDetailContext : IHasChanges, IHasValidationIssues, IPointDetailEditor, ICheckForChangesAndValidation
{
    private PointDetail _dbEntry;
    private Feature _detailData;
    private bool _hasChanges;
    private bool _hasValidationIssues;
    private StringDataEntryContext _noteEditor;
    private ContentFormatChooserContext _noteFormatEditor;
    private StatusControlContext _statusContext;
    private StringDataEntryContext _typeEditor;

    private FeaturePointDetailContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
    }

    public Feature DetailData
    {
        get => _detailData;
        set
        {
            if (Equals(value, _detailData)) return;
            _detailData = value;
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

    public StringDataEntryContext TypeEditor
    {
        get => _typeEditor;
        set
        {
            if (Equals(value, _typeEditor)) return;
            _typeEditor = value;
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

    public event PropertyChangedEventHandler PropertyChanged;

    public PointDetail CurrentPointDetail()
    {
        var newEntry = new PointDetail();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            newEntry.ContentId = Guid.NewGuid();
            newEntry.CreatedOn = DbEntry?.CreatedOn ?? DateTime.Now;
            if (newEntry.CreatedOn == DateTime.MinValue) newEntry.CreatedOn = DateTime.Now;
        }
        else
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
        }

        newEntry.DataType = DetailData.DataTypeIdentifier;

        var detailData = new Feature
        {
            Type = TypeEditor.UserValue.TrimNullToEmpty(),
            Notes = NoteEditor.UserValue.TrimNullToEmpty(),
            NotesContentFormat = NoteFormatEditor.SelectedContentFormatAsString
        };

        Db.DefaultPropertyCleanup(detailData);

        newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

        return newEntry;
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

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<FeaturePointDetailContext> CreateInstance(PointDetail detail,
        StatusControlContext statusContext)
    {
        var newControl = new FeaturePointDetailContext(statusContext);
        await newControl.LoadData(detail);
        return newControl;
    }

    public async Task LoadData(PointDetail toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = toLoad ?? new PointDetail {DataType = ((dynamic) DetailData).DataTypeIdentifier};

        if (!string.IsNullOrWhiteSpace(DbEntry.StructuredDataAsJson))
            DetailData = JsonSerializer.Deserialize<Feature>(DbEntry.StructuredDataAsJson);

        DetailData ??= new Feature {NotesContentFormat = UserSettingsUtilities.DefaultContentFormatChoice()};

        NoteEditor = StringDataEntryContext.CreateInstance();
        NoteEditor.Title = "Notes";
        NoteEditor.HelpText = "Notes";
        NoteEditor.ReferenceValue = DetailData.Notes ?? string.Empty;
        NoteEditor.UserValue = DetailData.Notes.TrimNullToEmpty();

        NoteFormatEditor = ContentFormatChooserContext.CreateInstance(StatusContext);
        NoteFormatEditor.InitialValue = DetailData.NotesContentFormat;
        await NoteFormatEditor.TrySelectContentChoice(DetailData.NotesContentFormat);

        TypeEditor = StringDataEntryContext.CreateInstance();
        TypeEditor.ValidationFunctions = new List<Func<string, Task<IsValid>>>
        {
            CommonContentValidation.ValidateFeatureType
        };
        TypeEditor.Title = "Type";
        TypeEditor.HelpText =
            "The type for this feature - this could be something unique or something recorded for many points";
        TypeEditor.ReferenceValue = DetailData.Type ?? string.Empty;
        TypeEditor.UserValue = DetailData.Type.TrimNullToEmpty();


        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (string.IsNullOrWhiteSpace(propertyName)) return;

        if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
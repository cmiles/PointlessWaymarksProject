using System.ComponentModel;
using System.Text.Json;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

[NotifyPropertyChanged]
public partial class FeaturePointDetailContext : IHasChanges, IHasValidationIssues,
    IPointDetailEditor,
    ICheckForChangesAndValidation
{
    private FeaturePointDetailContext(StatusControlContext? statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        DbEntry = PointDetail.CreateInstance();
        DetailData = new Feature();

        PropertyChanged += OnPropertyChanged;
    }

    public Feature DetailData { get; set; }
    public StringDataEntryContext? NoteEditor { get; set; }
    public ContentFormatChooserContext? NoteFormatEditor { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext? TypeEditor { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    //TODO: Eliminate this with Metalama
    public event PropertyChangedEventHandler? PropertyChanged;

    public PointDetail CurrentPointDetail()
    {
        var newEntry = PointDetail.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
        }

        newEntry.DataType = DetailData.DataTypeIdentifier;

        var detailData = new Feature
        {
            Type = TypeEditor!.UserValue.TrimNullToEmpty(),
            Notes = NoteEditor!.UserValue.TrimNullToEmpty(),
            NotesContentFormat = NoteFormatEditor!.SelectedContentFormatAsString
        };

        Db.DefaultPropertyCleanup(detailData);

        newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

        return newEntry;
    }

    public PointDetail DbEntry { get; set; }

    public static async Task<FeaturePointDetailContext> CreateInstance(PointDetail? detail,
        StatusControlContext statusContext)
    {
        var newControl = new FeaturePointDetailContext(statusContext);
        await newControl.LoadData(detail);
        return newControl;
    }

    public async Task LoadData(PointDetail? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = toLoad ?? PointDetail.CreateInstance();
        DbEntry.DataType = DetailData.DataTypeIdentifier;

        if (!string.IsNullOrWhiteSpace(DbEntry.StructuredDataAsJson))
        {
            var deserializedDetailData = JsonSerializer.Deserialize<Feature>(DbEntry.StructuredDataAsJson);
            if (deserializedDetailData != null) DetailData = deserializedDetailData;
        }

        NoteEditor = StringDataEntryContext.CreateInstance();
        NoteEditor.Title = "Notes";
        NoteEditor.HelpText = "Notes";
        NoteEditor.ReferenceValue = DetailData.Notes ?? string.Empty;
        NoteEditor.UserValue = DetailData.Notes.TrimNullToEmpty();

        NoteFormatEditor = await ContentFormatChooserContext.CreateInstance(StatusContext);
        NoteFormatEditor.InitialValue = DetailData.NotesContentFormat;
        await NoteFormatEditor.TrySelectContentChoice(DetailData.NotesContentFormat);

        TypeEditor = StringDataEntryContext.CreateInstance();
        TypeEditor.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
using System.ComponentModel;
using System.Text.Json;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

[NotifyPropertyChanged]
public partial class CampgroundPointDetailContext : IHasChanges, IHasValidationIssues,
    IPointDetailEditor,
    ICheckForChangesAndValidation
{
    private CampgroundPointDetailContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;

        DbEntry = PointDetail.CreateInstance();
        DetailData = new Campground();

        PropertyChanged += OnPropertyChanged;
    }

    public Campground DetailData { get; set; }
    public BoolNullableDataEntryContext? FeeEditor { get; set; }
    public StringDataEntryContext? NoteEditor { get; set; }
    public ContentFormatChooserContext? NoteFormatEditor { get; set; }
    public StatusControlContext StatusContext { get; set; }


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

        var detailData = new Campground
        {
            Notes = NoteEditor!.UserValue.TrimNullToEmpty(),
            NotesContentFormat = NoteFormatEditor!.SelectedContentFormatAsString
        };

        Db.DefaultPropertyCleanup(detailData);

        newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

        return newEntry;
    }

    public PointDetail DbEntry { get; set; }


    public static async Task<CampgroundPointDetailContext> CreateInstance(PointDetail? detail,
        StatusControlContext statusContext)
    {
        var newControl = new CampgroundPointDetailContext(statusContext);
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
            var deserializedDetailData = JsonSerializer.Deserialize<Campground>(DbEntry.StructuredDataAsJson);
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

        FeeEditor = BoolNullableDataEntryContext.CreateInstance();
        FeeEditor.Title = "Fee Area";
        FeeEditor.HelpText = "Is there a fee for using this parking area";
        FeeEditor.ReferenceValue = DetailData.Fee;
        FeeEditor.UserValue = DetailData.Fee;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
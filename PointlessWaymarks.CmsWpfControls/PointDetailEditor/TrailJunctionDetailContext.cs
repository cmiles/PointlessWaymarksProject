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
public partial class TrailJunctionPointDetailContext : IHasChanges, IHasValidationIssues, IPointDetailEditor,
    ICheckForChangesAndValidation
{
    private TrailJunctionPointDetailContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;

        DbEntry = PointDetail.CreateInstance();
        DetailData = new TrailJunction();

        PropertyChanged += OnPropertyChanged;
    }

    public TrailJunction DetailData { get; set; }
    public StringDataEntryContext? NoteEditor { get; set; }
    public ContentFormatChooserContext? NoteFormatEditor { get; set; }
    public BoolDataEntryContext? SignEditor { get; set; }
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

        var detailData = new TrailJunction
        {
            Notes = NoteEditor!.UserValue.TrimNullToEmpty(),
            NotesContentFormat = NoteFormatEditor!.SelectedContentFormatAsString,
            Sign = SignEditor!.UserValue
        };

        Db.DefaultPropertyCleanup(detailData);

        newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

        return newEntry;
    }

    public PointDetail DbEntry { get; set; }


    public static async Task<TrailJunctionPointDetailContext> CreateInstance(PointDetail? detail,
        StatusControlContext statusContext)
    {
        var newContext = new TrailJunctionPointDetailContext(statusContext);
        await newContext.LoadData(detail);
        return newContext;
    }

    public async Task LoadData(PointDetail? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = toLoad ?? PointDetail.CreateInstance();
        DbEntry.DataType = DetailData.DataTypeIdentifier;

        if (!string.IsNullOrWhiteSpace(DbEntry.StructuredDataAsJson))
        {
            var deserializedDetailData = JsonSerializer.Deserialize<TrailJunction>(DbEntry.StructuredDataAsJson);
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

        SignEditor = await BoolDataEntryContext.CreateInstance();
        SignEditor.Title = "Junction is Signed";
        SignEditor.HelpText = "There is some kind of sign at the junction";
        SignEditor.ReferenceValue = DetailData.Sign;
        SignEditor.UserValue = DetailData.Sign;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
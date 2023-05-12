using System.ComponentModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

public partial class ParkingPointDetailContext : ObservableObject, IHasChanges, IHasValidationIssues,
    IPointDetailEditor,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private PointDetail _dbEntry;
    [ObservableProperty] private Parking _detailData;
    [ObservableProperty] private BoolNullableDataEntryContext? _feeEditor;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private StringDataEntryContext? _noteEditor;
    [ObservableProperty] private ContentFormatChooserContext? _noteFormatEditor;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ParkingPointDetailContext(StatusControlContext? statusContext)
    {
        PropertyChanged += OnPropertyChanged;

        _dbEntry = PointDetail.CreateInstance();
        _detailData = new Parking();
        _statusContext = statusContext ?? new StatusControlContext();
    }


    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }


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

        var detailData = new Parking
        {
            Notes = NoteEditor!.UserValue.TrimNullToEmpty(),
            NotesContentFormat = NoteFormatEditor!.SelectedContentFormatAsString
        };

        Db.DefaultPropertyCleanup(detailData);

        newEntry.StructuredDataAsJson = JsonSerializer.Serialize(detailData);

        return newEntry;
    }

    public static async Task<ParkingPointDetailContext> CreateInstance(PointDetail? detail,
        StatusControlContext statusContext)
    {
        var newContext = new ParkingPointDetailContext(statusContext);
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
            var deserializedDetailData = JsonSerializer.Deserialize<Parking>(DbEntry.StructuredDataAsJson);
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using KellermanSoftware.CompareNetObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

public partial class PointDetailListContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private ObservableCollection<string> _additionalPointDetailTypes;
    [ObservableProperty] private List<PointDetail> _dbEntries;
    [ObservableProperty] private RelayCommand<IPointDetailEditor> _deleteDetailCommand;
    [ObservableProperty] private List<IPointDetailEditor> _deletedPointDetails;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private ObservableCollection<IPointDetailEditor> _items;
    [ObservableProperty] private RelayCommand<string> _loadNewDetailCommand;
    [ObservableProperty] private List<(string typeIdentifierAttribute, Type reflectedType)> _pointDetailTypeList;
    [ObservableProperty] private StatusControlContext _statusContext;

    private PointDetailListContext(StatusControlContext? statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;

        DeletedPointDetails = new List<IPointDetailEditor>();

        LoadNewDetailCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x => await LoadNewDetail(x));
        DeleteDetailCommand =
            StatusContext.RunNonBlockingTaskCommand<IPointDetailEditor>(async x => await DeleteDetail(x));
    }

    public void CheckForChangesAndValidationIssues()
    {
        var hasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || (Items?.Any(x => x.HasChanges) ?? false);

        if (!hasChanges && Items != null && DbEntries != null)
        {
            var originalItems = DbEntries.Select(x => x.ContentId).ToList();
            var listItems = Items.Select(x => x.DbEntry.ContentId).ToList();

            var logic = new CompareLogic { Config = { IgnoreCollectionOrder = true } };
            hasChanges = !logic.Compare(originalItems, listItems).AreEqual;
        }

        HasChanges = hasChanges;

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              (Items?.Any(x => x.HasValidationIssues) ?? false);
    }

    public static async Task<PointDetailListContext> CreateInstance(StatusControlContext? statusContext,
        PointContent? dbEntry)
    {
        var newControl = new PointDetailListContext(statusContext);
        await newControl.LoadData(dbEntry, true);
        return newControl;
    }

    public List<PointDetail> CurrentStateToPointDetailsList()
    {
        var returnList = new List<PointDetail>();
        if (Items == null || !Items.Any()) return returnList;
        return Items.Select(x => x.CurrentPointDetail()).ToList();
    }

    private async Task DeleteDetail(IPointDetailEditor pointDetail)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        pointDetail.PropertyChanged -= MonitorChildChangesAndValidations;

        Items.Remove(pointDetail);

        DeletedPointDetails.Add(pointDetail);

        CheckForChangesAndValidationIssues();
    }

    public async Task<IPointDetailEditor> ListItemEditorFromTypeIdentifier(PointDetail detail)
    {
        return detail.DataType switch
        {
            "Campground" => await CampgroundPointDetailContext.CreateInstance(detail, StatusContext),
            "Driving Directions" => await DrivingDirectionsPointDetailContext.CreateInstance(detail, StatusContext),
            "Feature" => await FeaturePointDetailContext.CreateInstance(detail, StatusContext),
            "Fee" => await FeePointDetailContext.CreateInstance(detail, StatusContext),
            "Parking" => await ParkingPointDetailContext.CreateInstance(detail, StatusContext),
            "Peak" => await PeakPointDetailContext.CreateInstance(detail, StatusContext),
            "Restroom" => await RestroomPointDetailContext.CreateInstance(detail, StatusContext),
            "Trail Junction" => await TrailJunctionPointDetailContext.CreateInstance(detail, StatusContext),
            _ => null
        };
    }

    public async Task LoadData(PointContent? dbEntry, bool clearState)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toLoad = new List<PointDetail>();

        if (dbEntry is { Id: > 0 })
        {
            var db = await Db.Context();

            toLoad = db.PointDetails.Where(x => x.PointContentId == dbEntry.ContentId).ToList();
        }

        DbEntries = toLoad.OrderBy(x => x.DataType).ToList();

        if (clearState)
        {
            DeletedPointDetails = new List<IPointDetailEditor>();

            var pointDetailTypes = from type in typeof(Db).Assembly.GetTypes()
                where typeof(IPointDetailData).IsAssignableFrom(type) && !type.IsInterface
                select type;

            _pointDetailTypeList = new List<(string typeIdentifierAttribute, Type reflectedType)>();

            foreach (var loopTypes in pointDetailTypes)
            {
                var typeExample = (IPointDetailData)Activator.CreateInstance(loopTypes);

                if (typeExample == null) continue;

                _pointDetailTypeList.Add((typeExample.DataTypeIdentifier, loopTypes));
            }

            _pointDetailTypeList = _pointDetailTypeList.OrderBy(x => x.typeIdentifierAttribute).ToList();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Items = new ObservableCollection<IPointDetailEditor>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopLoad in DbEntries)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var toAdd = await ListItemEditorFromTypeIdentifier(loopLoad);
            toAdd.PropertyChanged += MonitorChildChangesAndValidations;
            Items.Add(toAdd);

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        AdditionalPointDetailTypes =
            new ObservableCollection<string>(_pointDetailTypeList.Select(x => x.typeIdentifierAttribute));

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private async Task LoadNewDetail(string typeIdentifier)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(typeIdentifier))
        {
            StatusContext.ToastError("Detail Type is blank???");
            return;
        }

        var removedItems = DeletedPointDetails.Where(x => x.DbEntry.DataType == typeIdentifier).ToList();

        if (removedItems.Any())
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var toAdd = removedItems.First();

            DeletedPointDetails.Remove(toAdd);

            toAdd.PropertyChanged += MonitorChildChangesAndValidations;

            Items.Add(toAdd);

            CheckForChangesAndValidationIssues();

            return;
        }

        var newDetailEntry = _pointDetailTypeList.Where(x => x.typeIdentifierAttribute == typeIdentifier).ToList();

        if (!newDetailEntry.Any())
        {
            StatusContext.ToastError($"No Detail Type Found Matching {typeIdentifier}?");
            return;
        }

        if (newDetailEntry.Count > 1)
        {
            StatusContext.ToastError($"More than one Detail Type Found Matching {typeIdentifier}?");
            return;
        }

        var newPointDetail = new PointDetail { DataType = newDetailEntry.First().typeIdentifierAttribute };

        await ThreadSwitcher.ResumeForegroundAsync();

        var newDetail = await ListItemEditorFromTypeIdentifier(newPointDetail);
        newDetail.PropertyChanged += MonitorChildChangesAndValidations;

        Items.Add(newDetail);

        CheckForChangesAndValidationIssues();
    }

    private void MonitorChildChangesAndValidations(object sender, PropertyChangedEventArgs e)
    {
        if (e?.PropertyName == null) return;
        if (e.PropertyName.Contains("Changes")) CheckForChangesAndValidationIssues();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
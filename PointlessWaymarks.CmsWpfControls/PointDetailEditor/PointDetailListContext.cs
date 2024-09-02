using System.Collections.ObjectModel;
using System.ComponentModel;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PointDetailListContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    private PointDetailListContext(StatusControlContext statusContext, ObservableCollection<IPointDetailEditor> items,
        ObservableCollection<string> additionalPointDetailTypes)
    {
        StatusContext = statusContext;

        BuildCommands();

        DeletedPointDetails = [];

        DbEntries = [];
        DeletedPointDetails = [];
        PointDetailTypeList = [];
        AdditionalPointDetailTypes = additionalPointDetailTypes;
        Items = items;

        PropertyChanged += OnPropertyChanged;
    }

    public ObservableCollection<string> AdditionalPointDetailTypes { get; set; }
    public List<PointDetail> DbEntries { get; set; }
    public List<IPointDetailEditor> DeletedPointDetails { get; set; }
    public ObservableCollection<IPointDetailEditor> Items { get; set; }
    public List<(string typeIdentifierAttribute, Type reflectedType)> PointDetailTypeList { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        var hasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || Items.Any(x => x.HasChanges);

        if (!hasChanges)
        {
            var originalItems = DbEntries.Select(x => x.ContentId).ToList();
            var listItems = Items.Select(x => x.DbEntry.ContentId).ToList();

            var logic = new CompareLogic { Config = { IgnoreCollectionOrder = true } };
            hasChanges = !logic.Compare(originalItems, listItems).AreEqual;
        }

        HasChanges = hasChanges;

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this)
                              || Items.Any(x => x.HasValidationIssues);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static async Task<PointDetailListContext> CreateInstance(StatusControlContext? statusContext,
        PointContent dbEntry)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new PointDetailListContext(factoryContext,
            [], new ObservableCollectionListSource<string>());

        await ThreadSwitcher.ResumeBackgroundAsync();

        await newControl.LoadData(dbEntry, true);
        return newControl;
    }

    public List<PointDetail> CurrentStateToPointDetailsList()
    {
        var returnList = new List<PointDetail>();
        return !Items.Any() ? returnList : Items.Select(x => x.CurrentPointDetail()).ToList();
    }

    [NonBlockingCommand]
    private async Task DeleteDetail(IPointDetailEditor? pointDetail)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (pointDetail == null)
        {
            await StatusContext.ToastError("Nothing to Delete? Point Detail is null...");
            return;
        }

        pointDetail.PropertyChanged -= MonitorChildChangesAndValidations;

        Items.Remove(pointDetail);

        DeletedPointDetails.Add(pointDetail);

        CheckForChangesAndValidationIssues();
    }

    public async Task<IPointDetailEditor?> ListItemEditorFromTypeIdentifier(PointDetail detail)
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

    public async Task LoadData(PointContent dbEntry, bool clearState)
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
            DeletedPointDetails = [];

            var pointDetailTypes = from type in typeof(Db).Assembly.GetTypes()
                where typeof(IPointDetailData).IsAssignableFrom(type) && !type.IsInterface
                select type;

            PointDetailTypeList = [];

            foreach (var loopTypes in pointDetailTypes)
            {
                var typeExampleCreateInstance = Activator.CreateInstance(loopTypes);

                if (typeExampleCreateInstance is not IPointDetailData typeExample) continue;

                PointDetailTypeList.Add((typeExample.DataTypeIdentifier, loopTypes));
            }

            PointDetailTypeList = PointDetailTypeList.OrderBy(x => x.typeIdentifierAttribute).ToList();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Items = [];

        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopLoad in DbEntries)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var toAdd = await ListItemEditorFromTypeIdentifier(loopLoad);
            if (toAdd == null)
            {
                await StatusContext.ToastError("Unable to load Point Detail Type: " + loopLoad.DataType);
                continue;
            }

            toAdd.PropertyChanged += MonitorChildChangesAndValidations;
            Items.Add(toAdd);

            await ThreadSwitcher.ResumeBackgroundAsync();
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        AdditionalPointDetailTypes =
            new ObservableCollection<string>(PointDetailTypeList.Select(x => x.typeIdentifierAttribute));

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    [NonBlockingCommand]
    private async Task LoadNewDetail(string? typeIdentifier)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(typeIdentifier))
        {
            await StatusContext.ToastError("Detail Type is blank???");
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

        var newDetailEntry = PointDetailTypeList.Where(x => x.typeIdentifierAttribute == typeIdentifier).ToList();

        if (!newDetailEntry.Any())
        {
            await StatusContext.ToastError($"No Detail Type Found Matching {typeIdentifier}?");
            return;
        }

        if (newDetailEntry.Count > 1)
        {
            await StatusContext.ToastError($"More than one Detail Type Found Matching {typeIdentifier}?");
            return;
        }

        var newPointDetail = PointDetail.CreateInstance();
        newPointDetail.DataType = newDetailEntry.First().typeIdentifierAttribute;

        await ThreadSwitcher.ResumeForegroundAsync();

        var newDetail = await ListItemEditorFromTypeIdentifier(newPointDetail);
        if (newDetail == null)
        {
            await StatusContext.ToastError("Unable to load Point Detail?!?");
            return;
        }

        newDetail.PropertyChanged += MonitorChildChangesAndValidations;

        Items.Add(newDetail);

        CheckForChangesAndValidationIssues();
    }

    private void MonitorChildChangesAndValidations(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == null) return;
        if (e.PropertyName.Contains("Changes")) CheckForChangesAndValidationIssues();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
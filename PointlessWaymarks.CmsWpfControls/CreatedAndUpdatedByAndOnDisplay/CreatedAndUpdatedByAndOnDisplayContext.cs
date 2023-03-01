using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;

public partial class CreatedAndUpdatedByAndOnDisplayContext : ObservableObject, IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private string _createdAndUpdatedByAndOn;
    [ObservableProperty] private StringDataEntryContext _createdByEntry;
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private ICreatedAndLastUpdateOnAndBy _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private bool _isNewEntry;
    [ObservableProperty] private bool _showCreatedByEditor;
    [ObservableProperty] private bool _showUpdatedByEditor;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private StringDataEntryContext _updatedByEntry;
    [ObservableProperty] private DateTime? _updatedOn;

    private CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<CreatedAndUpdatedByAndOnDisplayContext> CreateInstance(StatusControlContext statusContext,
        ICreatedAndLastUpdateOnAndBy dbEntry)
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
        else if (((IContentId)DbEntry).Id < 1) IsNewEntry = true;

        CreatedByEntry = StringDataEntryContext.CreateInstance();
        CreatedByEntry.ValidationFunctions = new List<Func<string, Task<IsValid>>>
        {
            CommonContentValidation.ValidateCreatedBy
        };
        CreatedByEntry.Title = "Created By";
        CreatedByEntry.HelpText = "Created By Name";
        CreatedByEntry.ReferenceValue = string.IsNullOrWhiteSpace(toLoad?.CreatedBy) ? string.Empty : DbEntry.CreatedBy;
        CreatedByEntry.UserValue = string.IsNullOrWhiteSpace(toLoad?.CreatedBy)
            ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            : DbEntry.CreatedBy;


        UpdatedByEntry = StringDataEntryContext.CreateInstance();
        UpdatedByEntry.ValidationFunctions = new List<Func<string, Task<IsValid>>> { ValidateUpdatedBy };
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

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    public async Task<IsValid> ValidateUpdatedBy(string updatedBy)
    {
        if (!IsNewEntry && string.IsNullOrWhiteSpace(updatedBy))
            return new IsValid(false, "Updated by can not be blank when updating an entry");

        return new IsValid(true, string.Empty);
    }
}
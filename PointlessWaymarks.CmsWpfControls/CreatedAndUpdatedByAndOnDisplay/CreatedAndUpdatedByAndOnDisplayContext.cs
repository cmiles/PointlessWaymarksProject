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

    private CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext, ICreatedAndLastUpdateOnAndBy dbEntry, StringDataEntryContext createdByContext, StringDataEntryContext updatedByContext)
    {
        _statusContext = statusContext;

        PropertyChanged += OnPropertyChanged;

        _dbEntry = dbEntry;

        _isNewEntry = ((IContentId)DbEntry).Id < 1;

        _createdByEntry = createdByContext;
        _updatedByEntry = updatedByContext;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);

        //If this is a 'first update' go ahead and fill in the Created by as the updated by, this
        //is realistically just a trade off, better for most common workflow - potential mistake
        //if trading off created/updated authors since you are not 'forcing' an entry
        if (!IsNewEntry && string.IsNullOrWhiteSpace(UpdatedByEntry.UserValue))
        {
            UpdatedByEntry.ReferenceValue = CreatedByEntry.UserValue;
            UpdatedByEntry.UserValue = CreatedByEntry.UserValue;
        }

        _createdOn = dbEntry.CreatedOn;
        _updatedOn = dbEntry.LastUpdatedOn;

        var newStringParts = new List<string>();

        _createdAndUpdatedByAndOn = string.Empty;

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

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<CreatedAndUpdatedByAndOnDisplayContext> CreateInstance(StatusControlContext? statusContext,
        ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var factoryCreatedByContext = StringDataEntryContext.CreateInstance();
        factoryCreatedByContext.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            CommonContentValidation.ValidateCreatedBy
        };
        factoryCreatedByContext.Title = "Created By";
        factoryCreatedByContext.HelpText = "Created By Name";
        factoryCreatedByContext.ReferenceValue = string.IsNullOrWhiteSpace(dbEntry.CreatedBy) ? string.Empty : dbEntry.CreatedBy;
        factoryCreatedByContext.UserValue = string.IsNullOrWhiteSpace(dbEntry.CreatedBy)
            ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            : dbEntry.CreatedBy;


        var factoryUpdatedByEntry = StringDataEntryContext.CreateInstance();
        factoryUpdatedByEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>> {
            x =>
            {
                if (((IContentId)dbEntry).Id > 0 && string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "Updated by can not be blank when updating an entry"));

                return Task.FromResult(new IsValid(true, string.Empty));
            } };
        factoryUpdatedByEntry.Title = "Updated By";
        factoryUpdatedByEntry.HelpText = "Last Updated By Name";
        factoryUpdatedByEntry.ReferenceValue = dbEntry.LastUpdatedBy ?? string.Empty;
        factoryUpdatedByEntry.UserValue = dbEntry.LastUpdatedBy ?? string.Empty;

        var newInstance = new CreatedAndUpdatedByAndOnDisplayContext(factoryContext, dbEntry, factoryCreatedByContext, factoryUpdatedByEntry);

        return newInstance;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
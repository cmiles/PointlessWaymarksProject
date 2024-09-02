using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;

[NotifyPropertyChanged]
public partial class CreatedAndUpdatedByAndOnDisplayContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    private CreatedAndUpdatedByAndOnDisplayContext(StatusControlContext statusContext,
        ICreatedAndLastUpdateOnAndBy dbEntry, StringDataEntryContext createdByContext,
        StringDataEntryContext updatedByContext)
    {
        StatusContext = statusContext;

        DbEntry = dbEntry;

        IsNewEntry = ((IContentId)DbEntry).Id < 1;

        CreatedByEntry = createdByContext;
        UpdatedByEntry = updatedByContext;

        //If this is a 'first update' go ahead and fill in the Created by as the updated by, this
        //is realistically just a trade off, better for most common workflow - potential mistake
        //if trading off created/updated authors since you are not 'forcing' an entry
        if (!IsNewEntry && string.IsNullOrWhiteSpace(UpdatedByEntry.UserValue))
        {
            UpdatedByEntry.ReferenceValue = CreatedByEntry.UserValue;
            UpdatedByEntry.UserValue = CreatedByEntry.UserValue;
        }

        CreatedOn = dbEntry.CreatedOn;
        UpdatedOn = dbEntry.LastUpdatedOn;

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

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public string CreatedAndUpdatedByAndOn { get; set; }
    public StringDataEntryContext CreatedByEntry { get; set; }
    public DateTime? CreatedOn { get; set; }
    public ICreatedAndLastUpdateOnAndBy DbEntry { get; set; }
    public bool IsNewEntry { get; set; }
    public bool ShowCreatedByEditor { get; set; }
    public bool ShowUpdatedByEditor { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext UpdatedByEntry { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static async Task<CreatedAndUpdatedByAndOnDisplayContext> CreateInstance(StatusControlContext? statusContext,
        ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        var factoryCreatedByContext = StringDataEntryContext.CreateInstance();
        factoryCreatedByContext.ValidationFunctions = [CommonContentValidation.ValidateCreatedBy];
        factoryCreatedByContext.Title = "Created By";
        factoryCreatedByContext.HelpText = "Created By Name";
        factoryCreatedByContext.ReferenceValue =
            string.IsNullOrWhiteSpace(dbEntry.CreatedBy) ? string.Empty : dbEntry.CreatedBy;
        factoryCreatedByContext.UserValue = string.IsNullOrWhiteSpace(dbEntry.CreatedBy)
            ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            : dbEntry.CreatedBy;


        var factoryUpdatedByEntry = StringDataEntryContext.CreateInstance();
        factoryUpdatedByEntry.ValidationFunctions =
        [
            x =>
            {
                if (((IContentId)dbEntry).Id > 0 && string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "Updated by can not be blank when updating an entry"));

                return Task.FromResult(new IsValid(true, string.Empty));
            }
        ];
        factoryUpdatedByEntry.Title = "Updated By";
        factoryUpdatedByEntry.HelpText = "Last Updated By Name";
        factoryUpdatedByEntry.ReferenceValue = dbEntry.LastUpdatedBy ?? string.Empty;
        factoryUpdatedByEntry.UserValue = dbEntry.LastUpdatedBy ?? string.Empty;

        var newInstance = new CreatedAndUpdatedByAndOnDisplayContext(factoryContext, dbEntry, factoryCreatedByContext,
            factoryUpdatedByEntry);

        newInstance.CheckForChangesAndValidationIssues();

        return newInstance;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
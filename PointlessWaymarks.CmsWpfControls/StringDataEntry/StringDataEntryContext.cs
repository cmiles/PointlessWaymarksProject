using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.StringDataEntry;

public partial class StringDataEntryContext : ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private string _referenceValue;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _userValue;
    [ObservableProperty] private List<Func<string, Task<IsValid>>> _validationFunctions = new();
    [ObservableProperty] private string _validationMessage;

    private StringDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private async Task CheckForChangesAndValidationIssues()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions != null && ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = await loopValidations(UserValue);
                if (!validationResult.Valid)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationResult.Explanation;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static StringDataEntryContext CreateInstance()
    {
        return new();
    }

    public static async Task<StringDataEntryContext> CreateSlugInstance(ITitleSummarySlugFolder dbEntry)
    {
        var slugEntry = new StringDataEntryContext
        {
            Title = "Slug",
            HelpText = "This will be the Folder and File Name used in URLs - limited to a-z 0-9 _ -",
            ReferenceValue = dbEntry?.Slug ?? string.Empty,
            UserValue = StringTools.NullToEmptyTrim(dbEntry?.Slug),
            ValidationFunctions = new List<Func<string, Task<IsValid>>> { CommonContentValidation.ValidateSlugLocal }
        };

        await slugEntry.CheckForChangesAndValidationIssues();

        return slugEntry;
    }

    public static async Task<StringDataEntryContext> CreateSummaryInstance(ITitleSummarySlugFolder dbEntry)
    {
        var summaryEntry = new StringDataEntryContext
        {
            Title = "Summary",
            HelpText = "A short text entry that will show in Search and short references to the content",
            ReferenceValue = dbEntry?.Summary ?? string.Empty,
            UserValue = StringTools.NullToEmptyTrim(dbEntry?.Summary),
            ValidationFunctions = new List<Func<string, Task<IsValid>>> { CommonContentValidation.ValidateSummary }
        };

        await summaryEntry.CheckForChangesAndValidationIssues();

        return summaryEntry;
    }

    public static async Task<StringDataEntryContext> CreateTitleInstance(ITitleSummarySlugFolder dbEntry)
    {
        var titleEntry = new StringDataEntryContext
        {
            Title = "Title",
            HelpText = "Title Text",
            ReferenceValue = dbEntry?.Title ?? string.Empty,
            UserValue = StringTools.NullToEmptyTrim(dbEntry?.Title),
            ValidationFunctions = new List<Func<string, Task<IsValid>>> { CommonContentValidation.ValidateTitle }
        };

        await titleEntry.CheckForChangesAndValidationIssues();

        return titleEntry;
    }

    private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            await CheckForChangesAndValidationIssues();
    }
}
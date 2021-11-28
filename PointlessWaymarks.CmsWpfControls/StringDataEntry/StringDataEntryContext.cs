using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarks.CmsWpfControls.StringDataEntry;

[ObservableObject]
public partial class StringDataEntryContext : IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private string _referenceValue;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _userValue;
    [ObservableProperty] private List<Func<string, IsValid>> _validationFunctions = new();
    [ObservableProperty] private string _validationMessage;

    private StringDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void CheckForChangesAndValidationIssues()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions != null && ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = loopValidations(UserValue);
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

    public static StringDataEntryContext CreateSlugInstance(ITitleSummarySlugFolder dbEntry)
    {
        var slugEntry = new StringDataEntryContext
        {
            Title = "Slug",
            HelpText = "This will be the Folder and File Name used in URLs - limited to a-z 0-9 _ -",
            ReferenceValue = dbEntry?.Slug ?? string.Empty,
            UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Slug),
            ValidationFunctions = new List<Func<string, IsValid>> { CommonContentValidation.ValidateSlugLocal }
        };

        slugEntry.CheckForChangesAndValidationIssues();

        return slugEntry;
    }

    public static StringDataEntryContext CreateSummaryInstance(ITitleSummarySlugFolder dbEntry)
    {
        var summaryEntry = new StringDataEntryContext
        {
            Title = "Summary",
            HelpText = "A short text entry that will show in Search and short references to the content",
            ReferenceValue = dbEntry?.Summary ?? string.Empty,
            UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Summary),
            ValidationFunctions = new List<Func<string, IsValid>> { CommonContentValidation.ValidateSummary }
        };

        summaryEntry.CheckForChangesAndValidationIssues();

        return summaryEntry;
    }

    public static StringDataEntryContext CreateTitleInstance(ITitleSummarySlugFolder dbEntry)
    {
        var titleEntry = new StringDataEntryContext
        {
            Title = "Title",
            HelpText = "Title Text",
            ReferenceValue = dbEntry?.Title ?? string.Empty,
            UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Title),
            ValidationFunctions = new List<Func<string, IsValid>> { CommonContentValidation.ValidateTitle }
        };

        titleEntry.CheckForChangesAndValidationIssues();

        return titleEntry;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}
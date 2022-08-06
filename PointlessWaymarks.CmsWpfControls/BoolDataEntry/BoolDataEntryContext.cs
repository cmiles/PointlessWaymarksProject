using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarks.CmsWpfControls.BoolDataEntry;

[ObservableObject]
public partial class BoolDataEntryContext : IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _helpText;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _referenceValue;
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _userValue;
    [ObservableProperty] private List<Func<bool, IsValid>> _validationFunctions = new();
    [ObservableProperty] private string _validationMessage;

    private BoolDataEntryContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public bool UserValueIsNullable => false;

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue != ReferenceValue;

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

    public static BoolDataEntryContext CreateInstance()
    {
        return new BoolDataEntryContext();
    }

    public static BoolDataEntryContext CreateInstanceForIsDraft(IMainSiteFeed dbEntry, bool defaultSetting)
    {
        var newContext = new BoolDataEntryContext
        {
            ReferenceValue = dbEntry?.IsDraft ?? defaultSetting,
            UserValue = dbEntry?.IsDraft ?? defaultSetting,
            Title = "Draft",
            HelpText =
                "'Draft' content will not appear in the Main Site Feed, Search or RSS Feeds - however html will " +
                "still be generated for the content, this is NOT a way to keep content hidden or secret!"
        };

        return newContext;
    }

    public static BoolDataEntryContext CreateInstanceForShowInMainSiteFeed(IMainSiteFeed dbEntry, bool defaultSetting)
    {
        var newContext = new BoolDataEntryContext
        {
            ReferenceValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting,
            UserValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting,
            Title = "Show in Main Site Feed",
            HelpText =
                "Checking this box will make the content appear in the Main Site RSS Feed and - if the content is recent - on the site's homepage"
        };

        return newContext;
    }

    public static BoolDataEntryContext CreateInstanceForShowInSearch(IShowInSearch dbEntry, bool defaultSetting)
    {
        var newContext = new BoolDataEntryContext
        {
            ReferenceValue = dbEntry?.ShowInSearch ?? defaultSetting,
            UserValue = dbEntry?.ShowInSearch ?? defaultSetting,
            Title = "Show in Search",
            HelpText =
                "If checked the content will appear in Site, Tag and other search screens - otherwise the content will still be " +
                "on the site and publicly available but it will not show in search"
        };

        return newContext;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}
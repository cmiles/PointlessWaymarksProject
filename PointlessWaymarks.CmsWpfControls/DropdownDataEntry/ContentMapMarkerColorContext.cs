using System.Collections.ObjectModel;
using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.StringWithDropdownDataEntry;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.DropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentMapMarkerColorContext : IDropdownDataEntryContext
{
    private ContentMapMarkerColorContext(StatusControlContext statusContext, Func<Task<List<string>>> loader, PointContent dbEntry)
    {
        StatusContext = statusContext;

        Title = "Marker Color";
        HelpText =
            "An optional Color for the map icon.";

        GetCurrentMapMarkerColors = loader;

        ExistingChoices = new ObservableCollection<string>(PointContent.MapMarkerColorChoices().OrderBy(x =>x));
        ReferenceValue = dbEntry.MapMarkerColor ?? string.Empty;
        UserValue = dbEntry.MapMarkerColor ?? string.Empty;

        ValidationFunctions = new List<Func<string?, IsValid>>();

        PropertyChanged += OnPropertyChanged;
    }

    public ObservableCollection<string> ExistingChoices { get; set; }
    public Func<Task<List<string>>> GetCurrentMapMarkerColors { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public string HelpText { get; set; }
    public string? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public string? UserValue { get; set; }
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;

    private void CheckForChangesAndValidate()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var (passed, validationMessage) = loopValidations(UserValue);
                if (!passed)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationMessage;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static async Task<ContentMapMarkerColorContext> CreateInstance(StatusControlContext? statusContext,
        PointContent dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var loader = () => Task.FromResult(PointContent.MapMarkerColorChoices());

        await ThreadSwitcher.ResumeForegroundAsync();

        var newControl = new ContentMapMarkerColorContext(factoryContext, loader, dbEntry);

        newControl.CheckForChangesAndValidate();

        return newControl;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidate();
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.DropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentMapMarkerColorContext : IDropdownDataEntryContext
{
    private ContentMapMarkerColorContext(StatusControlContext statusContext,
        Func<Task<List<DropDownDataChoice>>> loader,
        PointContent dbEntry)
    {
        StatusContext = statusContext;

        Title = "Marker Color";
        HelpText =
            "An optional Color for the map icon.";

        GetCurrentMapMarkerColors = loader;

        ExistingChoices = new ObservableCollection<DropDownDataChoice>(ColorChoices());
        ReferenceValue = dbEntry.MapMarkerColor ?? string.Empty;
        UserValue = dbEntry.MapMarkerColor ?? string.Empty;

        ValidationFunctions = [];

        PropertyChanged += OnPropertyChanged;
    }

    public Func<Task<List<DropDownDataChoice>>> GetCurrentMapMarkerColors { get; set; }
    public List<Func<string?, IsValid>> ValidationFunctions { get; set; }
    public ObservableCollection<DropDownDataChoice> ExistingChoices { get; set; }
    public string HelpText { get; set; }
    public string? ReferenceValue { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; }
    public string? UserValue { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

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

    private static List<DropDownDataChoice> ColorChoices()
    {
        return PointContent.MapMarkerColorChoicesDictionary()
            .Select(x => new DropDownDataChoice { DisplayString = x.Key, DataString = x.Value })
            .OrderBy(x => x.DisplayString).ToList();
    }

    private static Task<List<DropDownDataChoice>> ColorChoicesAsync()
    {
        return Task.FromResult(PointContent.MapMarkerColorChoicesDictionary()
            .Select(x => new DropDownDataChoice { DisplayString = x.Key, DataString = x.Value })
            .OrderBy(x => x.DisplayString).ToList());
    }

    public static async Task<ContentMapMarkerColorContext> CreateInstance(StatusControlContext? statusContext,
        PointContent dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var loader = ColorChoicesAsync;

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
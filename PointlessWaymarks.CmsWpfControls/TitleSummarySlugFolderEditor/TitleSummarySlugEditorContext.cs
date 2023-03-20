using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFolder;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;

[ObservableObject]
public partial class TitleSummarySlugEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private Func<TitleSummarySlugEditorContext, bool>? _customTitleCheckToEnable;
    [ObservableProperty] private RelayCommand? _customTitleCommand;
    [ObservableProperty] private bool _customTitleFunctionEnabled;
    [ObservableProperty] private string? _customTitleFunctionText;
    [ObservableProperty] private bool _customTitleFunctionVisible;
    [ObservableProperty] private ITitleSummarySlugFolder _dbEntry;
    [ObservableProperty] private ContentFolderContext _folderEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private StringDataEntryContext _slugEntry;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private StringDataEntryContext _summaryEntry;
    [ObservableProperty] private StringDataEntryContext _titleEntry;
    [ObservableProperty] private RelayCommand _titleToSlugCommand;
    [ObservableProperty] private bool _titleToSlugEnabled = true;
    [ObservableProperty] private RelayCommand _titleToSummaryCommand;
    [ObservableProperty] private bool _titleToSummaryEnabled = true;

    private TitleSummarySlugEditorContext(StatusControlContext statusContext, ITitleSummarySlugFolder dbEntry, StringDataEntryContext slugEntry, StringDataEntryContext summaryEntry, StringDataEntryContext titleEntry, ContentFolderContext folderContext, string? customTitleCommandText, RelayCommand? customTitleCommand, Func<TitleSummarySlugEditorContext, bool>? customTitleCheckToEnable)
    {
        _statusContext = statusContext;

        _customTitleFunctionText = customTitleCommandText;
        _customTitleCommand = customTitleCommand;
        _customTitleCheckToEnable = customTitleCheckToEnable;
        _customTitleFunctionVisible = !string.IsNullOrWhiteSpace(CustomTitleFunctionText) && CustomTitleCommand is not null && CustomTitleCheckToEnable is not null;

        _titleToSlugCommand = StatusContext.RunBlockingActionCommand(TitleToSlug);
        _titleToSummaryCommand = StatusContext.RunBlockingActionCommand(TitleToSummary);

        _dbEntry = dbEntry;

        _slugEntry = slugEntry;
        _summaryEntry = summaryEntry;
        _titleEntry = titleEntry;
        _folderEntry = folderContext;

        TitleEntry.PropertyChanged += TitleChangedMonitor;
        SlugEntry.PropertyChanged += TitleChangedMonitor;
        SummaryEntry.PropertyChanged += TitleChangedMonitor;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext? statusContext,
        ITitleSummarySlugFolder dbEntry, string? customTitleCommandText, RelayCommand? customTitleCommand,
        Func<TitleSummarySlugEditorContext, bool>? customTitleCheckToEnable)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var factoryTitleEntry = await StringDataEntryTypes.CreateTitleInstance(dbEntry);

        var factorySlugEntry = await StringDataEntryTypes.CreateSlugInstance(dbEntry);

        var factorySummaryEntry = await StringDataEntryTypes.CreateSummaryInstance(dbEntry);

        var factoryFolderEntry = await ContentFolderContext.CreateInstance(factoryContext, dbEntry);

        var newItem = new TitleSummarySlugEditorContext(factoryContext, dbEntry, factorySlugEntry, factorySummaryEntry, factoryTitleEntry, factoryFolderEntry, customTitleCommandText, customTitleCommand, customTitleCheckToEnable);

        return newItem;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public void CheckForChangesToTitleToFunctionStates()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TitleEntry.UserValue))
            {
                TitleToSlugEnabled = false;
                TitleToSummaryEnabled = false;
                return;
            }

            TitleToSlugEnabled = SlugTools.CreateSlug(true, TitleEntry.UserValue) != SlugEntry.UserValue;
            TitleToSummaryEnabled =
                !(SummaryEntry.UserValue.Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase) ||
                  (SummaryEntry.UserValue.Length - 1 == TitleEntry.UserValue.Length &&
                   char.IsPunctuation(SummaryEntry.UserValue[^1]) && SummaryEntry.UserValue[..^1]
                       .Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase)));

            CustomTitleFunctionEnabled = CustomTitleCheckToEnable?.Invoke(this) ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    private void TitleChangedMonitor(object? sender, PropertyChangedEventArgs e)
    {
        if (!e.PropertyName?.Equals("UserValue") ?? true) return;

        CheckForChangesToTitleToFunctionStates();
    }

    public void TitleToSlug()
    {
        SlugEntry.UserValue = SlugTools.CreateSlug(true, TitleEntry.UserValue);
    }

    public void TitleToSummary()
    {
        SummaryEntry.UserValue = TitleEntry.UserValue;

        if (!char.IsPunctuation(SummaryEntry.UserValue[^1]))
            SummaryEntry.UserValue += ".";
    }
}
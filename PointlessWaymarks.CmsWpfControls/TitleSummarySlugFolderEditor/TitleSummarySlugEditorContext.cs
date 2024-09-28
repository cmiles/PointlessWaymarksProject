using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using ContentFolderContext = PointlessWaymarks.CmsWpfControls.StringWithDropdownDataEntry.ContentFolderContext;

namespace PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TitleSummarySlugEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    private TitleSummarySlugEditorContext(StatusControlContext statusContext, ITitleSummarySlugFolder dbEntry,
        StringDataEntryContext slugEntry, StringDataEntryContext summaryEntry, StringDataEntryContext titleEntry,
        ContentFolderContext folderContext, string? customTitleCommandText, RelayCommand? customTitleCommand,
        Func<TitleSummarySlugEditorContext, bool>? customTitleCheckToEnable)
    {
        StatusContext = statusContext;

        BuildCommands();

        CustomTitleFunctionText = customTitleCommandText;
        CustomTitleCommand = customTitleCommand;
        CustomTitleCheckToEnable = customTitleCheckToEnable;
        CustomTitleFunctionVisible = !string.IsNullOrWhiteSpace(CustomTitleFunctionText) &&
                                     CustomTitleCommand is not null && CustomTitleCheckToEnable is not null;

        DbEntry = dbEntry;

        SlugEntry = slugEntry;
        SummaryEntry = summaryEntry;
        TitleEntry = titleEntry;
        FolderEntry = folderContext;

        PropertyChanged += OnPropertyChanged;

        TitleEntry.PropertyChanged += TitleChangedMonitor;
        SlugEntry.PropertyChanged += TitleChangedMonitor;
        SummaryEntry.PropertyChanged += TitleChangedMonitor;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public bool AllAvailableTitleActionsEnabled { get; set; }
    public Func<TitleSummarySlugEditorContext, bool>? CustomTitleCheckToEnable { get; set; }
    public RelayCommand? CustomTitleCommand { get; set; }
    public bool CustomTitleFunctionEnabled { get; set; }
    public string? CustomTitleFunctionText { get; set; }
    public bool CustomTitleFunctionVisible { get; set; }
    public ITitleSummarySlugFolder DbEntry { get; set; }
    public ContentFolderContext FolderEntry { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public StringDataEntryContext SlugEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext SummaryEntry { get; set; }
    public StringDataEntryContext TitleEntry { get; set; }
    public bool TitleToSlugEnabled { get; set; } = true;
    public bool TitleToSummaryEnabled { get; set; } = true;

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    [BlockingCommand]
    public async Task AllAvailableTitleActions()
    {
        if (TitleToSlugEnabled) await TitleToSlug();
        if (TitleToSummaryEnabled) await TitleToSummary();
        if (CustomTitleFunctionEnabled) CustomTitleCommand?.Execute(null);
    }

    public void CheckForChangesToTitleToFunctionStates()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TitleEntry.UserValue))
            {
                TitleToSlugEnabled = false;
                TitleToSummaryEnabled = false;
                CustomTitleFunctionEnabled = CustomTitleCheckToEnable?.Invoke(this) ?? false;
                AllAvailableTitleActionsEnabled =
                    TitleToSlugEnabled || TitleToSummaryEnabled || CustomTitleFunctionEnabled;
                return;
            }

            TitleToSlugEnabled = SlugTools.CreateSlug(true, TitleEntry.UserValue) != SlugEntry.UserValue;

            if(string.IsNullOrWhiteSpace(SummaryEntry.UserValue)) TitleToSummaryEnabled = true;
            else
                TitleToSummaryEnabled =
                !(SummaryEntry.UserValue.Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase) ||
                  (SummaryEntry.UserValue.Length - 1 == TitleEntry.UserValue.Length &&
                   char.IsPunctuation(SummaryEntry.UserValue[^1]) && SummaryEntry.UserValue[..^1]
                       .Equals(TitleEntry.UserValue, StringComparison.OrdinalIgnoreCase)));

            CustomTitleFunctionEnabled = CustomTitleCheckToEnable?.Invoke(this) ?? false;

            AllAvailableTitleActionsEnabled =
                TitleToSlugEnabled || TitleToSummaryEnabled || CustomTitleFunctionEnabled;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext? statusContext,
        ITitleSummarySlugFolder dbEntry, string? customTitleCommandText, RelayCommand? customTitleCommand,
        Func<TitleSummarySlugEditorContext, bool>? customTitleCheckToEnable)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryTitleEntry = await StringDataEntryTypes.CreateTitleInstance(dbEntry);

        var factorySlugEntry = await StringDataEntryTypes.CreateSlugInstance(dbEntry);

        var factorySummaryEntry = await StringDataEntryTypes.CreateSummaryInstance(dbEntry);

        var factoryFolderEntry = await ContentFolderContext.CreateInstance(factoryStatusContext, dbEntry);

        var newItem = new TitleSummarySlugEditorContext(factoryStatusContext, dbEntry, factorySlugEntry, factorySummaryEntry,
            factoryTitleEntry, factoryFolderEntry, customTitleCommandText, customTitleCommand,
            customTitleCheckToEnable);

        newItem.CheckForChangesAndValidationIssues();

        newItem.CheckForChangesToTitleToFunctionStates();

        return newItem;
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

    [BlockingCommand]
    public Task TitleToSlug()
    {
        SlugEntry.UserValue = SlugTools.CreateSlug(true, TitleEntry.UserValue);
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public Task TitleToSummary()
    {
        SummaryEntry.UserValue = TitleEntry.UserValue;

        if (!char.IsPunctuation(SummaryEntry.UserValue[^1]))
            SummaryEntry.UserValue += ".";
        return Task.CompletedTask;
    }
}
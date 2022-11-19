using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFolder;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;

[ObservableObject]
public partial class TitleSummarySlugEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private Func<TitleSummarySlugEditorContext, bool> _customTitleCheckToEnable;
    [ObservableProperty] private RelayCommand _customTitleCommand;
    [ObservableProperty] private bool _customTitleFunctionEnabled;
    [ObservableProperty] private string _customTitleFunctionText;
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

    private TitleSummarySlugEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        PropertyChanged += OnPropertyChanged;
    }

    private TitleSummarySlugEditorContext(StatusControlContext statusContext, string customTitleCommandText,
        RelayCommand customTitleCommand, Func<TitleSummarySlugEditorContext, bool> customTitleCheckToEnable)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        CustomTitleFunctionText = customTitleCommandText;
        CustomTitleCommand = customTitleCommand;
        CustomTitleCheckToEnable = customTitleCheckToEnable;
        CustomTitleFunctionVisible = true;
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
            TitleToSlugEnabled = SlugTools.Create(true, TitleEntry.UserValue) != SlugEntry.UserValue;
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

    public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext statusContext,
        ITitleSummarySlugFolder dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = new TitleSummarySlugEditorContext(statusContext);
        await newItem.LoadData(dbEntry);

        return newItem;
    }

    public static async Task<TitleSummarySlugEditorContext> CreateInstance(StatusControlContext statusContext,
        string customTitleCommandText, RelayCommand customTitleCommand,
        Func<TitleSummarySlugEditorContext, bool> customTitleCheckToEnable, ITitleSummarySlugFolder dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = new TitleSummarySlugEditorContext(statusContext, customTitleCommandText, customTitleCommand,
            customTitleCheckToEnable);
        await newItem.LoadData(dbEntry);

        return newItem;
    }

    public async Task LoadData(ITitleSummarySlugFolder dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        TitleToSlugCommand = StatusContext.RunBlockingActionCommand(TitleToSlug);
        TitleToSummaryCommand = StatusContext.RunBlockingActionCommand(TitleToSummary);

        DbEntry = dbEntry;

        TitleEntry = await StringDataEntryContext.CreateTitleInstance(DbEntry);
        TitleEntry.PropertyChanged += TitleChangedMonitor;

        SlugEntry = await StringDataEntryContext.CreateSlugInstance(DbEntry);
        SlugEntry.PropertyChanged += TitleChangedMonitor;

        SummaryEntry = await StringDataEntryContext.CreateSummaryInstance(DbEntry);
        SummaryEntry.PropertyChanged += TitleChangedMonitor;

        FolderEntry = await ContentFolderContext.CreateInstance(StatusContext, DbEntry);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    private void TitleChangedMonitor(object sender, PropertyChangedEventArgs e)
    {
        if (!e?.PropertyName?.Equals("UserValue") ?? true) return;

        CheckForChangesToTitleToFunctionStates();
    }

    public void TitleToSlug()
    {
        SlugEntry.UserValue = SlugTools.Create(true, TitleEntry.UserValue);
    }

    public void TitleToSummary()
    {
        SummaryEntry.UserValue = TitleEntry.UserValue;

        if (!char.IsPunctuation(SummaryEntry.UserValue[^1]))
            SummaryEntry.UserValue += ".";
    }
}
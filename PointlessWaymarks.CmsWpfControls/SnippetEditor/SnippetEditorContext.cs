using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.CmsWpfControls.SnippetEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SnippetEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;

    public SnippetEditorContext(StatusControlContext statusContext, Snippet dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public StringDataEntryContext? BodyEntry { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public Snippet DbEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }

    public string SnippetEditorHelpText =>
        """
        Snippets can be used to create reusable content that can be inserted into other content with the advantage that they can be updated once and the changes will be reflected into all the content that uses the snippet. Snippets can contain other bracket codes and other snippets - however be careful not to create circular references.
        """;

    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext? SummaryEntry { get; set; }
    public StringDataEntryContext? TitleEntry { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }

    public bool HasValidationIssues { get; set; }

    public static async Task<SnippetEditorContext> CreateInstance(StatusControlContext? statusContext,
        Snippet? toLoad = null)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new SnippetEditorContext(factoryStatusContext,
            NewContentModels.InitializeSnippet(toLoad));
        await newContext.LoadData(toLoad);
        return newContext;
    }


    private Snippet CurrentStateToSnippetContent()
    {
        var newEntry = Snippet.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.Title = TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.BodyContent = BodyEntry!.UserValue.TrimNullToEmpty();

        return newEntry;
    }

    public async Task LoadData(Snippet? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeSnippet(toLoad);

        TitleEntry = StringDataEntryContext.CreateInstance();

        TitleEntry.Title = "Title";
        TitleEntry.HelpText = "Title Text";
        TitleEntry.ReferenceValue = DbEntry?.Title ?? string.Empty;
        TitleEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry?.Title);
        TitleEntry.ValidationFunctions = [CommonContentValidation.ValidateTitle];

        SummaryEntry = StringDataEntryContext.CreateInstance();

        SummaryEntry.Title = "Summary";
        SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
        SummaryEntry.ReferenceValue = DbEntry?.Summary ?? string.Empty;
        SummaryEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry?.Summary);
        SummaryEntry.ValidationFunctions = [CommonContentValidation.ValidateSummary];

        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);

        BodyEntry = StringDataEntryContext.CreateInstance();

        BodyEntry.Title = "Snippet Text";
        BodyEntry.HelpText = "Text that will be placed anywhere {{snippet [this ContentId]}} is found";
        BodyEntry.ReferenceValue = DbEntry?.BodyContent ?? string.Empty;
        BodyEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry?.BodyContent);

        HelpContext = new HelpDisplayContext([
            SnippetEditorHelpText
        ]);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerate(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerate(true);
    }

    public async Task SaveAndGenerate(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await SnippetGenerator.SaveAndGenerateHtml(CurrentStateToSnippetContent(),
            StatusContext.ProgressTracker());

        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }

        await LoadData(newContent);

        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }
}